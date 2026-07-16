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
/// Возврат похороненного сообщения в обработку на низком уровне: одного статуса мало, нужны ещё
/// ScheduledStartIndexing и сброс Retries. Ровно этот согласованный сброс инкапсулирует
/// <see cref="Dex.Cap.Inbox.Interfaces.IInboxDeadLetterService"/> (см. InboxRequeueTests); здесь он
/// проверяется по колонкам напрямую, чтобы зафиксировать, почему сброс обязан быть согласованным.
/// </summary>
public class InboxDeadLetterRecoveryTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestErrorInboxCommandHandler.ProcessAttempts = 0;
    }

    [Test]
    public async Task DeadLettered_AfterDocumentedReset_IsProcessedAgain()
    {
        var sp = InitInboxServiceCollection(retries: 1)
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, TestErrorInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestErrorInboxCommand { Args = "poison" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        var buried = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.DeadLettered, buried.Status);

        // Только статуса недостаточно: выборка требует ScheduledStartIndexing IS NOT NULL,
        // а Retries на пределе похоронит сообщение на первом же отказе.
        using (var partialScope = sp.CreateScope())
        {
            await partialScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, InboxMessageStatus.New));
        }

        Assert.AreEqual(0, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None),
            "status alone is not enough: the message stays out of the fetch set");

        // Полный рецепт из README: статус, Retries и ScheduledStartIndexing.
        TestErrorInboxCommandHandler.ProcessAttempts = 0;

        using (var fullScope = sp.CreateScope())
        {
            await fullScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.New)
                    .SetProperty(x => x.Retries, 0)
                    .SetProperty(x => x.ScheduledStartIndexing, x => x.StartAtUtc));
        }

        Assert.AreEqual(1, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None),
            "the documented reset must return the message to processing");
        Assert.AreEqual(1, TestErrorInboxCommandHandler.ProcessAttempts);
    }
}
