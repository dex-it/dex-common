using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dex.Cap.Outbox.AspNetScheduler;
using Npgsql;

namespace Dex.Cap.ConsoleTest
{
    class Program
    {
        private static readonly AsyncLocal<string> AsyncLocal = new();

        static async Task Main()
        {
            var s = Timeout.InfiniteTimeSpan.ToString();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var sp = InitServiceCollection(configuration)
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 5;
            TestCommandHandler.OnProcess += (sender, args) =>
            {
                if (Interlocked.Decrement(ref count) < 0)
                {
                    throw new InvalidOperationException("Multiple execution detected!");
                }
            };

            // prepare DB
            var db = sp.GetRequiredService<TestDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var logger = sp.GetService<ILogger<Program>>();
            var client = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var activity = new Activity("TestActivity-1");
            activity.Start();
            logger.LogDebug("DEBUG...");
            logger.LogInformation("INFO...");

            for (var i = 0; i < count; i++)
            {
                var a = new Activity("TestActivity-2");
                a.SetParentId(activity.Id);
                a.Start();
                await client.EnqueueAsync(Guid.NewGuid(), new TestOutboxCommand { Args = "hello world " + i }, CancellationToken.None);
                await db.SaveChangesAsync();
                a.Stop();
            }

            activity.Stop();

            for (var i = 0; i < 20; i++)
            {
                _ = Task.Run(async () =>
                {
                    AsyncLocal.Value = "Thread_" + i;

                    while (true)
                    {
                        using var scope = sp.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                        try
                        {
                            await handler.ProcessAsync();
                        }
                        catch (InvalidOperationException ex) when (ex.InnerException?.InnerException is PostgresException { SqlState: "40001" })
                        {
                            logger.LogDebug("...");
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(exception, "Can't process message");
                        }

                        Thread.Sleep(10_000);
                    }
                });

                Thread.Sleep(50);
            }

            Thread.Sleep(-1);
        }

        private static IServiceCollection InitServiceCollection(IConfigurationRoot configuration)
        {
            return new ServiceCollection()
                .AddLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddConfiguration(configuration.GetSection("Logging"));
                    lb.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
                    lb.AddConsole(options => options.IncludeScopes = true);

                    // lb.SetMinimumLevel(LogLevel.Debug);
                    // lb.AddFilter("Microsoft", LogLevel.None);
                })
                .AddDbContext<TestDbContext>(o =>
                {
                    //
                    o.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                })
                .AddOutbox<TestDbContext>()
                .RegisterOutboxScheduler();
        }
    }
}