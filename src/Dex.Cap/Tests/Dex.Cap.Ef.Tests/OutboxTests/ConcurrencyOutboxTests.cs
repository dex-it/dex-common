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
        public async Task RunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world " + count++ }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world " + count++ }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "concurrency world " + count++ }, CancellationToken.None);
            await SaveChanges(sp);

            TestCommandHandler.OnProcess += (_, _) => Interlocked.Decrement(ref count);

            var tasks = new List<Task>();
            for (var i = 0; i < 30; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = sp.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                    await handler.ProcessAsync();
                }));

                Thread.Sleep(2);
            }

            Task.WaitAll(tasks.ToArray());

            TestContext.WriteLine("TestCompleted:" + count);

            var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var otb = db.Set<OutboxEnvelope>().All(x => x.CorrelationId == correlationId && x.Retries == 1);
            Assert.IsNotNull(otb);
            Assert.AreEqual(0, count);
        }
    }
}