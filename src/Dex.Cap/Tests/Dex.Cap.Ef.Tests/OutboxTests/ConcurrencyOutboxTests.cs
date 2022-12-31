using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class ConcurrencyOutboxTests : BaseTest
    {
        [Test]
        public async Task MultipleOutboxHandlersRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            const int expected = 4;
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world - 1" }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world - 3" }, CancellationToken.None);
            await SaveChanges(sp);

            TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var tasks = new List<Task>();
            for (var i = 0; i < 30; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = sp.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                    while (count < expected)
                    {
                        await handler.ProcessAsync();
                        await Task.Delay(50);
                    }
                }));

                Thread.Sleep(2);
            }

            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world - 2" }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world - 4" }, CancellationToken.None);
            await SaveChanges(sp);

            Task.WaitAll(tasks.ToArray());

            TestContext.WriteLine("TestCompleted:" + count);

            var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var envelopes = db.Set<OutboxEnvelope>().ToArray();
            Assert.IsTrue(envelopes.All(x => x.Status == OutboxMessageStatus.Succeeded && x.Retries == 1));
            Assert.AreEqual(expected, count);
        }
    }
}