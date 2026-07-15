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
    public async Task Cleanup_RemovesOldSucceededAndKeepsDeadLettered()
    {
        var sp = InitInboxServiceCollection(retries: 1)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, TestErrorInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        await inboxService.EnqueueAsync(new TestInboxCommand(), new InboxMessageIdentity("ok-1", "consumer-1"));
        await inboxService.EnqueueAsync(new TestErrorInboxCommand(), new InboxMessageIdentity("fail-1", "consumer-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Состариваем обе строки, чтобы обе попали под порог ретеншена.
        using (var ageScope = sp.CreateScope())
        {
            var ageDb = ageScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await ageDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedUtc, DateTime.UtcNow.AddDays(-10)));
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db, scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(1, removed);

        var left = await db.Set<InboxEnvelope>().ToListAsync();
        Assert.AreEqual(1, left.Count);

        // DeadLettered переживает чистку: это сообщения для ручного разбора, стирать их молча нельзя.
        Assert.AreEqual(InboxMessageStatus.DeadLettered, left[0].Status);
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
