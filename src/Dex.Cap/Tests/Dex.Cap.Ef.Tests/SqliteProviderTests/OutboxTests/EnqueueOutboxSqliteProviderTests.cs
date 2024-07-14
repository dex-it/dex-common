using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.SqliteProviderTests.OutboxTests;

public class EnqueueOutboxSqliteProviderTests : SqliteBaseTest
{
    [Test]
    [TestCase(1_000, 1)]
    [TestCase(60_000, 0)] // Может быть рассинхронизация времени с БД.
    public async Task CleanupSuccess(int olderThanMilliseconds, int expectedDeletedMessages)
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<SqliteTestDbContext>>()
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService<SqliteTestDbContext>>();
        await outboxService.EnqueueAsync(Guid.NewGuid(), new TestOutboxCommand { Args = "hello world" });
        await SaveChanges(sp);

        var count = 0;
        TestCommandHandler.OnProcess += (_, _) => { count++; };

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync();

        Assert.AreEqual(1, count);

        await Task.Delay(2000);

        var cleaner = sp.GetRequiredService<IOutboxCleanupDataProvider>();
        var deletedMessages = await cleaner.Cleanup(TimeSpan.FromMilliseconds(olderThanMilliseconds), CancellationToken.None);

        Assert.AreEqual(expectedDeletedMessages, deletedMessages);
    }

}