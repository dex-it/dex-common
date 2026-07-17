using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Ef;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
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
        // Имена строк отражают их итоговую судьбу: эта за две попытки дойдёт до DeadLettered.
        await inboxService.EnqueueAsync(new TestErrorInboxCommand(), new InboxMessageIdentity("dead-1", "consumer-1"));

        // Первый цикл: успех уходит в Succeeded, падение в Failed (попытка ещё есть, retries: 2).
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Добавляется свежая падающая строка уже после первого цикла: ей предстоит одна попытка, поэтому
        // она останется в Failed, а не будет похоронена.
        var stillFailingId = new InboxMessageIdentity("fail-1", "consumer-1");
        await inboxService.EnqueueAsync(new TestErrorInboxCommand(), stillFailingId);

        // Второй (и единственный здесь) цикл: у "dead-1" исчерпывается вторая попытка, и она уходит в
        // DeadLettered; "fail-1" впервые падает и остаётся в Failed.
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
            db,
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

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
    public async Task Cleanup_MoreRowsThanOneBatch_RemovesThemAll()
    {
        // Цикл чистки останавливается по неполной пачке (limit = 1000). Ошибка в условии выхода либо
        // оставила бы хвост навсегда, либо закрутила бы бесконечный цикл.
        const int rows = 2345;

        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        using (var seedScope = sp.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var stale = DateTime.UtcNow.AddDays(-10);

            for (var i = 0; i < rows; i++)
            {
                var envelope = new InboxEnvelope(Guid.NewGuid(), $"m-{i}", "consumer-1", TestInboxCommand.InboxTypeId, "{}")
                {
                    Status = InboxMessageStatus.Succeeded,
                    ScheduledStartIndexing = null,
                    CreatedUtc = stale
                };

                seedDb.Set<InboxEnvelope>().Add(envelope);
            }

            await seedDb.SaveChangesAsync();
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db,
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(rows, removed, "every eligible row must be removed, not just the first batch");
        Assert.AreEqual(0, await db.Set<InboxEnvelope>().CountAsync());
    }

    [Test]
    public async Task Cleanup_RowHeldByAnotherWriter_SkipsItAndStillDrainsTheTail()
    {
        // Строку держит чужая незакоммиченная транзакция. SKIP LOCKED пропускает такую строку, поэтому
        // пачка приходит КОРОЧЕ лимита. Выход из цикла по неполной пачке принял бы это за «чистить больше
        // нечего» и бросил бы весь хвост до следующего запуска, то есть на час.
        // Заодно фиксируем: чистка НЕ ждёт чужого писателя. Иначе долгая внешняя транзакция подвешивала
        // бы её на неопределённое время, а пропущенная строка всё равно удаляется следующим заходом.
        const int rows = 2345;

        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var stale = DateTime.UtcNow.AddDays(-10);

        using (var seedScope = sp.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<TestDbContext>();

            for (var i = 0; i < rows; i++)
            {
                // Разные CreatedUtc: порядок обязан быть детерминированным, иначе неизвестно,
                // попала ли подопытная строка в первую пачку.
                seedDb.Set<InboxEnvelope>().Add(
                    new InboxEnvelope(Guid.NewGuid(), $"m-{i}", "consumer-1", TestInboxCommand.InboxTypeId, "{}")
                    {
                        Status = InboxMessageStatus.Succeeded,
                        ScheduledStartIndexing = null,
                        CreatedUtc = stale.AddSeconds(i)
                    });
            }

            await seedDb.SaveChangesAsync();
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db,
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        // Внешний писатель отдельным соединением: именно так выглядит внеполосная правка, а заодно
        // EF-транзакции здесь недоступны из-за EnableRetryOnFailure в тестовом контексте.
        await using var blocker = new NpgsqlConnection(db.Database.GetConnectionString());
        await blocker.OpenAsync();
        await using var blockerTx = await blocker.BeginTransactionAsync();

        // Самая старая строка, то есть гарантированно первая в первой пачке.
        await using (var command = blocker.CreateCommand())
        {
            command.Transaction = blockerTx;
            command.CommandText = """UPDATE cap.inbox SET "ErrorMessage" = 'moved' WHERE "MessageId" = 'm-0'""";
            await command.ExecuteNonQueryAsync();
        }

        // Чистка идёт, пока блокировка ДЕРЖИТСЯ: она обязана не ждать её, а пропустить строку.
        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(rows - 1, removed, "a short batch must not be mistaken for an empty queue");
        Assert.AreEqual(1, await db.Set<InboxEnvelope>().CountAsync(), "only the locked row may be left behind");

        // Писатель отпустил строку: следующий заход обязан её забрать, иначе она осталась бы навсегда.
        await blockerTx.CommitAsync();

        Assert.AreEqual(1, await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None));
        Assert.AreEqual(0, await db.Set<InboxEnvelope>().CountAsync(), "the skipped row must not be left forever");
    }

    [Test]
    public async Task Cleanup_TwoCleanersInParallel_RemoveEveryRowExactlyOnce()
    {
        // Чистильщик живёт на КАЖДОЙ реплике, поэтому параллельный запуск это норма, а не экзотика.
        // Без SKIP LOCKED реплики выбирают одни и те же строки: проигравший ждёт победителя, получает
        // пустой результат и по условию выхода решает, что чистить больше нечего, бросая хвост на час.
        const int rows = 3000;

        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var stale = DateTime.UtcNow.AddDays(-10);

        using (var seedScope = sp.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<TestDbContext>();

            for (var i = 0; i < rows; i++)
            {
                seedDb.Set<InboxEnvelope>().Add(
                    new InboxEnvelope(Guid.NewGuid(), $"m-{i}", "consumer-1", TestInboxCommand.InboxTypeId, "{}")
                    {
                        Status = InboxMessageStatus.Succeeded,
                        ScheduledStartIndexing = null,
                        CreatedUtc = stale.AddSeconds(i)
                    });
            }

            await seedDb.SaveChangesAsync();
        }

        // Каждому чистильщику свой scope: это разные реплики, а не общий DbContext.
        var cleanups = Enumerable.Range(0, 2).Select(async _ =>
        {
            using var scope = sp.CreateScope();
            var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
                scope.ServiceProvider.GetRequiredService<TestDbContext>(),
                scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
                Options.Create(new InboxHandlerOptions()),
                scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

            return await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);
        });

        var removed = await Task.WhenAll(cleanups);

        using var checkScope = sp.CreateScope();
        var db = checkScope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.AreEqual(0, await db.Set<InboxEnvelope>().CountAsync(), "two cleaners must not leave a tail behind");
        Assert.AreEqual(rows, removed.Sum(), "every row must be counted by exactly one cleaner");
    }

    [Test]
    public async Task Cleanup_DoesNotTouchMessagesOfAnotherService()
    {
        // Ретеншен это окно дедупликации. Вычистив чужие строки, сервис молча укоротил бы окно соседа
        // и тот начал бы принимать повторы как новые сообщения.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var stale = DateTime.UtcNow.AddDays(-10);

        using (var seedScope = sp.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<TestDbContext>();

            // Своё сообщение и чужое: обработчик зарегистрирован только у TestInboxCommand.
            foreach (var discriminator in new[] { TestInboxCommand.InboxTypeId, TestErrorInboxCommand.InboxTypeId })
            {
                seedDb.Set<InboxEnvelope>().Add(
                    new InboxEnvelope(Guid.NewGuid(), $"m-{discriminator}", "consumer-1", discriminator, "{}")
                    {
                        Status = InboxMessageStatus.Succeeded,
                        ScheduledStartIndexing = null,
                        CreatedUtc = stale
                    });
            }

            await seedDb.SaveChangesAsync();
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            db,
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(1, removed, "only this service's message is eligible");

        var left = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(TestErrorInboxCommand.InboxTypeId, left.MessageType,
            "another service's deduplication key must survive this service's cleanup");
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
            db,
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(1), CancellationToken.None);

        Assert.AreEqual(0, removed);

        // Пока строка жива, повторная доставка того же сообщения распознаётся как дубль.
        var status = await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("ok-1", "consumer-1"));
        Assert.AreEqual(InboxEnqueueStatus.Duplicate, status);
    }
}