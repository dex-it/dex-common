using System;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Dex.Cap.ConsoleTest
{
    class Program
    {
        static readonly AsyncLocal<string> _asyncLocal = new();

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

            var logger = sp.GetService<ILogger<Program>>();
            logger.LogDebug("DEBUG...");
            logger.LogInformation("INFO...");

            var client = sp.GetRequiredService<IOutboxService>();
            await client.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, CancellationToken.None);
            await client.EnqueueAsync(new TestOutboxCommand { Args = "hello world2" }, CancellationToken.None);
            await Save(sp);

            for (var i = 0; i < 5; i++)
            {
                _ = Task.Run(async () =>
                {
                    _asyncLocal.Value = "Thread_" + i;

                    while (true)
                    {
                        using var scope = sp.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                        try
                        {
                            await handler.ProcessAsync();
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
                    lb.AddConsole();
                    lb.AddConfiguration(configuration.GetSection("Logging"));
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

        private static async Task Save(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<TestDbContext>();
            db.Database.EnsureCreated();
            await db.SaveChangesAsync();
        }
    }
}