using System;
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
/// Атомарность бизнес-эффекта и статуса — суть паттерна. Если эффект коммитится, а статус нет,
/// сообщение будет обработано повторно и эффект применится дважды.
/// </summary>
public class InboxAtomicityTests : BaseTest
{
    [Test]
    public async Task Process_HandlerSucceeds_EffectAndStatusAreCommittedTogether()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxUserCommand { UserName = "atomic-user" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.AreEqual(1, await db.Users.CountAsync(x => x.Name == "atomic-user"));

        var envelope = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status);
    }

    [Test]
    public async Task Process_HandlerThrowsAfterEffect_EffectIsRolledBackAndMessageIsRetried()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxUserCommand { UserName = "rolled-back-user", ThrowAfterEffect = true },
            new InboxMessageIdentity("message-1", "consumer-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Обработчик успел создать пользователя, но упал: транзакция откатилась целиком.
        Assert.AreEqual(0, await db.Users.CountAsync(x => x.Name == "rolled-back-user"));

        // Сам факт неудачи при этом сохранён: он пишется отдельной транзакцией уже после отката.
        var envelope = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Failed, envelope.Status);
        Assert.AreEqual(1, envelope.Retries);
        Assert.IsNotNull(envelope.ScheduledStartIndexing);
        Assert.IsNull(envelope.LockId);
        Assert.IsTrue(envelope.Error!.Contains("Handler failed after the business effect", StringComparison.Ordinal));
    }

    [Test]
    public async Task Process_HandlerThrowsThenSucceeds_EffectIsAppliedExactlyOnce()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        await inboxService.EnqueueAsync(
            new TestInboxUserCommand { UserName = "retried-user", ThrowAfterEffect = true },
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Первая попытка падает и откатывается.
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Подменяем тело на успешное, имитируя устранение причины сбоя, и повторяем.
        using (var fixScope = sp.CreateScope())
        {
            var fixDb = fixScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await fixDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Content, """{"UserName":"retried-user","ThrowAfterEffect":false}""")
                    // Тест про атомарность, а не про тайминг повтора. Делаем строку заведомо наступившей, чтобы
                    // второй ProcessAsync забрал её при ЛЮБОЙ стратегии повторов. Иначе тест молча зависел бы от
                    // нулевой задержки дефолтной стратегии: смена дефолта на Incremental/Exponential увела бы
                    // StartAtUtc в будущее, строка не была бы выбрана, и падение выглядело бы как регресс
                    // атомарности, а не как смена задержки.
                    .SetProperty(x => x.StartAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Откат первой попытки означает, что эффект применён ровно один раз, а не дважды.
        Assert.AreEqual(1, await db.Users.CountAsync(x => x.Name == "retried-user"));

        var envelope = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status);
    }
}