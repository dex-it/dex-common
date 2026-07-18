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
/// Потеря аренды во время обработки: эффект обработчика не должен закоммититься без смены статуса,
/// иначе следующий владелец аренды применит его второй раз.
/// </summary>
public class InboxLeaseLossTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestInboxUserCommandHandler.OnProcessed = null;
    }

    [TearDown]
    public void ResetHook()
    {
        TestInboxUserCommandHandler.OnProcessed = null;
    }

    [Test]
    public async Task Process_LeaseStolenDuringHandler_EffectIsRolledBackAndMessageStaysForTheNewOwner()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxUserCommand { UserName = "victim" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Имитируем перехват аренды другим инстансом ровно между работой обработчика и фиксацией успеха:
        // отдельный DbContext, чтобы UPDATE не попал в транзакцию обработки.
        TestInboxUserCommandHandler.OnProcessed = async () =>
        {
            using var thief = sp.CreateScope();
            await thief.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockId, Guid.NewGuid()));
        };

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var db = GetDb(sp);

        // Эффект обработчика обязан быть откачен вместе с попыткой зафиксировать успех.
        Assert.AreEqual(0, await db.Users.CountAsync(), "handler effect must not be committed without the status");

        var envelope = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.New, envelope.Status, "the message must stay exactly as it was");

        // Попытка не потрачена: обработку завершит владелец аренды, а не этот инстанс.
        Assert.AreEqual(0, envelope.Retries, "losing the lease must not spend an attempt");
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set");
    }

    [Test]
    public async Task Process_LeaseExpiredDuringHandler_EffectIsRolledBack()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxUserCommand { UserName = "victim" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Аренда протухает по часам БД, но LockId остаётся нашим: предикат владения обязан это заметить.
        TestInboxUserCommandHandler.OnProcessed = async () =>
        {
            using var scope = sp.CreateScope();
            await scope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockExpirationTimeUtc, DateTime.UtcNow.AddMinutes(-1)));
        };

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        var db = GetDb(sp);
        Assert.AreEqual(0, await db.Users.CountAsync(), "handler effect must not be committed without the status");

        var envelope = await db.Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.New, envelope.Status, "the message must stay exactly as it was");
        Assert.AreEqual(0, envelope.Retries, "losing the lease must not spend an attempt");
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set");
    }

    [Test]
    public async Task Process_LeaseHeld_EffectAndStatusAreCommittedTogether()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxUserCommand>, TestInboxUserCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxUserCommand { UserName = "healthy" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Контрольный прогон: без потери аренды эффект и статус коммитятся вместе.
        var db = GetDb(sp);
        Assert.AreEqual(1, await db.Users.CountAsync());
        Assert.AreEqual(InboxMessageStatus.Succeeded, (await db.Set<InboxEnvelope>().SingleAsync()).Status);
    }
}