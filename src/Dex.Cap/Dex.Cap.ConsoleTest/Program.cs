using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dex.Cap.Outbox.Scheduler;

namespace Dex.Cap.ConsoleTest
{
    class Program
    {
        static async Task Main()
        {
            var s = Timeout.InfiniteTimeSpan.ToString();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();

            var sp = InitServiceCollection(configuration)
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            //await client.Enqueue(new TestOutboxCommand { Args = "hello world" }, CancellationToken.None);
            //await client.Enqueue(new TestOutboxCommand { Args = "hello world2" }, CancellationToken.None);

            await Save(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var cts = new CancellationTokenSource();

            for (int i = 0; i < 2; i++)
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        using var scope = sp.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();

                        try
                        {
                            await handler.ProcessAsync(cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        Thread.Sleep(3000);
                    }
                });
                //Thread.Sleep(500);
            }

            Thread.Sleep(-1);
        }

        private static IServiceCollection InitServiceCollection(IConfigurationRoot configuration)
        {
            return new ServiceCollection()
                .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Trace))
                .AddDbContext<TestDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")))
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
