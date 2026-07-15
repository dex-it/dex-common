using System;
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
/// Строка с непригодным LockTimeout не должна останавливать инбокс. Такое значение может появиться только
/// внеполосной записью: конструктор конверта проверяет минимум, у колонки есть дефолт.
/// </summary>
public class InboxPoisonedRowTests : BaseTest
{
    [Test]
    public async Task Process_RowWithLockTimeoutBelowMinimum_IsDeadLetteredAndTheBatchSurvives()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "good-1" }, new InboxMessageIdentity("good-1", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "good-2" }, new InboxMessageIdentity("good-2", "c-1"));

        // Внеполосная запись в обход конструктора: ровно так строка и становится непригодной.
        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .Where(x => x.MessageId == "poison")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        // Не бросает: одна непригодная строка не имеет права ронять сборку партии.
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(2, processed, "healthy messages of the batch must still be processed");

        var envelopes = await GetDb(sp).Set<InboxEnvelope>().ToListAsync();

        foreach (var messageId in new[] { "good-1", "good-2" })
        {
            Assert.AreEqual(InboxMessageStatus.Succeeded, envelopes.Single(x => x.MessageId == messageId).Status,
                $"{messageId} must be handled despite the poisoned row");
        }

        // Непригодная строка похоронена: сама она не починится, и таскать её из цикла в цикл бессмысленно.
        var poisoned = envelopes.Single(x => x.MessageId == "poison");
        Assert.AreEqual(InboxMessageStatus.DeadLettered, poisoned.Status);
        Assert.IsNull(poisoned.ScheduledStartIndexing, "the poisoned row must leave the fetch set");
        Assert.IsNull(poisoned.LockId, "the lease must be released");
        Assert.IsTrue(poisoned.ErrorMessage!.Contains("LockTimeout", StringComparison.Ordinal));
    }

    [Test]
    public async Task Process_PoisonedRow_DoesNotStallTheInboxOnTheNextCycle()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));

        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        var handler = sp.GetRequiredService<IInboxHandler>();
        Assert.AreEqual(0, await handler.ProcessAsync(CancellationToken.None));

        // Ключевое: строка похоронена, поэтому следующий цикл её не перезахватывает и не падает снова.
        // Раньше здесь был вечный LogCritical при формально поднятом хосте.
        Assert.AreEqual(0, await handler.ProcessAsync(CancellationToken.None));

        // Новое сообщение обрабатывается как ни в чём не бывало.
        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "fresh" }, new InboxMessageIdentity("fresh", "c-1"));

        Assert.AreEqual(1, await handler.ProcessAsync(CancellationToken.None), "the inbox must keep working");
        Assert.AreEqual(InboxMessageStatus.Succeeded,
            (await GetDb(sp).Set<InboxEnvelope>().SingleAsync(x => x.MessageId == "fresh")).Status);
    }
}
