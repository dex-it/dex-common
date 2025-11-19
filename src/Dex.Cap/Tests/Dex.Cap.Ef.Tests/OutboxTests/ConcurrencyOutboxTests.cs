using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

public class ConcurrencyOutboxTests : BaseTest
{
    [TestCase(1, 1, 10)]
    [TestCase(1, 1, 100)]
    [TestCase(10, 10, 100)]
    [TestCase(10, 10, 1000)]
    [TestCase(100, 10, 1000)]
    public async Task MultipleOutboxHandlersRunTest(int messageToProcess, int concurrentLimit, int allMessageCount)
    {
        var sp = InitServiceCollection(messageToProcess, concurrentLimit)
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        // init messages before
        var outboxService = sp.GetRequiredService<IOutboxService>();
        await outboxService.EnqueueAsync(new TestOutboxCommand {Args = "concurrency world - 1"});
        await outboxService.EnqueueAsync(new TestOutboxCommand {Args = "concurrency world - 2"});

        // add extra messages
        for (var i = 2; i < allMessageCount; i++)
            await outboxService.EnqueueAsync(new TestOutboxCommand {Args = "concurrency world - " + i});

        await SaveChanges(sp);

        // run handlers
        const int handlerCount = 8;
        var tasks = new List<Task>(handlerCount);
        for (var i = 0; i < handlerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = sp.CreateScope();
                var db = GetDb(scope.ServiceProvider);

                var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                while (db.Set<OutboxEnvelope>().Any(x => x.Status == OutboxMessageStatus.New))
                {
                    await handler.ProcessAsync();
                    await Task.Delay(50);
                }
            }));

            Thread.Sleep(2);
        }

        // wait for complete
        Task.WaitAll(tasks.ToArray());

        using var scope = sp.CreateScope();
        var db = GetDb(scope.ServiceProvider);

        if (db.Set<OutboxEnvelope>().Any(x => x.Status == OutboxMessageStatus.Failed))
            NUnit.Framework.Assert.Fail("has failed results");

        var envelopes = db.Set<OutboxEnvelope>().ToList();
        Assert.IsTrue(envelopes.All(x => x is {Status: OutboxMessageStatus.Succeeded, Retries: 1}));
        Assert.AreEqual(allMessageCount, envelopes.Count);
    }
}