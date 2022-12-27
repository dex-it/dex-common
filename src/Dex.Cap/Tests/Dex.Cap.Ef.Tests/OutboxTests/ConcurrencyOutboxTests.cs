using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class ConcurrencyOutboxTests : BaseTest
    {
        [Test]
        public async Task RunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var repeat = 10;
            while (repeat-- > 0)
            {
                var outboxService = sp.GetRequiredService<IOutboxService>();
                var correlationId = Guid.NewGuid();
                await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world " + repeat }, CancellationToken.None);
                await SaveChanges(sp);

                var count = 0;
                TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);

                var tasks = Enumerable.Range(1, 26).Select(_ => Task.Run(async () =>
                {
                    using var scope = sp.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                    await handler.ProcessAsync();
                }));

                Task.WaitAll(tasks.ToArray());

                TestContext.WriteLine("TestCompleted:" + count);
                Assert.AreEqual(1, count);
            }
        }
    }
}