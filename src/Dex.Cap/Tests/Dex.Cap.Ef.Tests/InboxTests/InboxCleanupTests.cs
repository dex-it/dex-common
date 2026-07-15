using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Ef;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

public class InboxCleanupTests : BaseTest
{
    [Test]
    public async Task Cleanup_RemovesOnlyOldSucceeded_AndKeepsFailedAndDeadLettered()
    {
        // retries: 2, чтобы получить строку, застрявшую в Failed между попытками: чистка обязана
        // обходить её стороной, иначе сообщение, ждущее ретрая, исчезнет молча.
        var sp = InitInboxServiceCollection(retries: 2)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, TestErrorInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        await inboxService.EnqueueAsync(new TestInboxCommand(), new InboxMessageIdentity("ok-1", "consumer-1"));
        await inboxService.EnqueueAsync(new TestErrorInboxCommand(), new InboxMessageIdentity("fail-1", "consumer-1"));

        // Первый цикл: успех уходит в Succeeded, падение в Failed (попытки ещё есть).
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        var failedId = new InboxMessageIdentity("dead-1", "consumer-1");
        await inboxService.EnqueueAsync(new TestErrorInboxCommand(), failedId);

        // Второй и третий циклы добивают обе падающие строки: первая уходит в DeadLettered,
        // вторая остаётся в Failed.
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        using (var ageScope = sp.CreateScope())
        {
            var ageDb = ageScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await ageDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedUtc, DateTime.UtcNow.AddDays(-10)));
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var statusesBefore = await db.Set<InboxEnvelope>().Select(x => x.Status).ToListAsync();
        Assert.IsTrue(statusesBefore.Contains(InboxMessageStatus.Succeeded), "setup must produce a Succeeded row");
        Assert.IsTrue(statusesBefore.Contains(InboxMessageStatus.Failed), "setup must produce a Failed row");
        Assert.IsTrue(statusesBefore.Contains(InboxMessageStatus.DeadLettered), "setup must produce a DeadLettered row");

        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db, scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(1, removed, "only the Succeeded row is old enough and eligible");

        var left = await db.Set<InboxEnvelope>().Select(x => x.Status).ToListAsync();

        // Succeeded удалён, а Failed и DeadLettered живы: первое ещё ждёт ретрая, второе ждёт разбора.
        Assert.AreEqual(2, left.Count);
        Assert.IsFalse(left.Contains(InboxMessageStatus.Succeeded));
        Assert.IsTrue(left.Contains(InboxMessageStatus.Failed), "a message awaiting a retry must survive cleanup");
        Assert.IsTrue(left.Contains(InboxMessageStatus.DeadLettered), "a buried message must survive cleanup");
    }

    [Test]
    public async Task Cleanup_KeepsRecentSucceeded_BecauseRetentionIsTheDeduplicationWindow()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("ok-1", "consumer-1"));
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db, scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(0, removed);

        // Пока строка жива, повторная доставка того же сообщения распознаётся как дубль.
        var status = await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("ok-1", "consumer-1"));
        Assert.AreEqual(InboxEnqueueStatus.Duplicate, status);
    }
}
