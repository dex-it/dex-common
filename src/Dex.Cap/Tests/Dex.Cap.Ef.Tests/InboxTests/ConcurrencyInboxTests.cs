using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Главный тест конкурентности: конкурирующие обработчики не должны обработать одно сообщение дважды.
/// Именно отсутствие атомарного захвата запрещает горизонтальное масштабирование сервиса.
/// </summary>
public class ConcurrencyInboxTests : BaseTest
{
    [TestCase(1, 1, 10)]
    [TestCase(10, 10, 100)]
    [TestCase(100, 10, 300)]
    public async Task MultipleInboxHandlersRunTest(int messageToProcess, int concurrentLimit, int allMessageCount)
    {
        var sp = InitInboxServiceCollection(messageToProcess, concurrentLimit)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        for (var i = 0; i < allMessageCount; i++)
        {
            await inboxService.EnqueueAsync(
                new TestInboxCommand { Args = "concurrency world - " + i },
                new InboxMessageIdentity("message-" + i, "consumer-1"));
        }

        var processedIds = new ConcurrentBag<Guid>();
        TestInboxCommandHandler.OnProcess += OnProcess;

        try
        {
            const int handlerCount = 8;
            var tasks = new List<Task>(handlerCount);

            for (var i = 0; i < handlerCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                    var handler = scope.ServiceProvider.GetRequiredService<IInboxHandler>();

                    while (await db.Set<InboxEnvelope>().AnyAsync(x => x.ScheduledStartIndexing != null))
                    {
                        await handler.ProcessAsync();
                        await Task.Delay(50);
                    }
                }));

                Thread.Sleep(2);
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            TestInboxCommandHandler.OnProcess -= OnProcess;
        }

        using var checkScope = sp.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var envelopes = await checkDb.Set<InboxEnvelope>().ToListAsync();

        Assert.AreEqual(allMessageCount, envelopes.Count);
        Assert.IsTrue(envelopes.All(x => x.Status == InboxMessageStatus.Succeeded), "all messages must succeed");
        Assert.IsTrue(envelopes.All(x => x.Retries == 0), "success must not consume retries");

        // Ключевой инвариант: ровно одна обработка на сообщение при 8 конкурентных обработчиках.
        Assert.AreEqual(allMessageCount, processedIds.Count);
        Assert.AreEqual(allMessageCount, processedIds.Distinct().Count());

        void OnProcess(object? _, TestInboxCommand message) => processedIds.Add(message.TestId);
    }
}
