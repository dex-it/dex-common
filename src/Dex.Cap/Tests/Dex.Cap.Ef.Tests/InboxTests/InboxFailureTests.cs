using System;
using System.Linq;
using System.Text.Json;
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

public class InboxFailureTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestErrorInboxCommandHandler.ProcessAttempts = 0;
    }

    [Test]
    public async Task Process_AlwaysFailingHandler_RetriesThenGoesToDeadLetter()
    {
        const int retries = 3;

        var sp = InitInboxServiceCollection(retries: retries)
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, TestErrorInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestErrorInboxCommand { Args = "poison" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        var handler = sp.GetRequiredService<IInboxHandler>();

        // Каждый цикл — одна попытка. Сообщение остаётся в выборке, пока попытки не исчерпаны.
        for (var i = 0; i < retries; i++)
        {
            var processed = await handler.ProcessAsync(CancellationToken.None);
            Assert.AreEqual(1, processed, $"attempt {i + 1} must fetch the message");
        }

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.DeadLettered, envelope.Status);
        Assert.AreEqual(retries, envelope.Retries);
        Assert.AreEqual(retries, TestErrorInboxCommandHandler.ProcessAttempts);

        // Похороненное сообщение выведено из выборки: бесконечно долбить его нельзя.
        Assert.IsNull(envelope.ScheduledStartIndexing);
        Assert.IsTrue(envelope.Error!.Contains("Handler always fails", StringComparison.Ordinal));

        var afterDeadLetter = await handler.ProcessAsync(CancellationToken.None);
        Assert.AreEqual(0, afterDeadLetter, "dead lettered message must not be fetched again");
        Assert.AreEqual(retries, TestErrorInboxCommandHandler.ProcessAttempts);
    }

    [Test]
    public async Task Process_CorruptedPayload_IsRetriedAsAnOrdinaryFailure()
    {
        // retries: 3, чтобы пройти путь Failed -> повтор, а не сразу DeadLettered: битое тело
        // не особый случай, это обычный отказ обработки.
        var sp = InitInboxServiceCollection(retries: 3)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "valid" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Портим тело так, как это происходит при несовместимом изменении схемы сообщения.
        using (var breakScope = sp.CreateScope())
        {
            var breakDb = breakScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await breakDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Content, "{ this is not json"));
        }

        // Не бросает: битое тело это исход сообщения, а не авария процесса. Ровно на этом
        // самописный инбокс роняет весь под и уходит в краш-луп.
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Failed, envelope.Status, "the first failure must be retriable, not terminal");
        Assert.AreEqual(1, envelope.Retries);
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set for a retry");

        // Ассерт по содержимому: без него тест зелёный при ЛЮБОЙ ошибке и не доказывает,
        // что сработала именно ветка разбора тела.
        Assert.IsTrue(envelope.Error!.Contains(nameof(JsonException), StringComparison.Ordinal),
            $"the outcome must record the deserialization failure, but was: {envelope.ErrorMessage}");
    }

    [Test]
    public async Task Process_CorruptedPayload_EventuallyGoesToDeadLetter()
    {
        var sp = InitInboxServiceCollection(retries: 1)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "valid" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        using (var breakScope = sp.CreateScope())
        {
            var breakDb = breakScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await breakDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Content, "{ this is not json"));
        }

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Тело само не починится, поэтому по исчерпании попыток сообщение обязано быть похоронено,
        // а не крутиться вечно.
        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.DeadLettered, envelope.Status);
        Assert.IsNull(envelope.ScheduledStartIndexing);
        Assert.IsTrue(envelope.Error!.Contains(nameof(JsonException), StringComparison.Ordinal));
    }

    [Test]
    public async Task Process_ExpiredLease_MessageIsFetchedAgain()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(),
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Имитируем инстанс, умерший посреди обработки: аренда захвачена и уже протухла.
        using (var lockScope = sp.CreateScope())
        {
            var lockDb = lockScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await lockDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LockId, Guid.NewGuid())
                    .SetProperty(x => x.LockExpirationTimeUtc, DateTime.UtcNow.AddMinutes(-1)));
        }

        // Истёкшая аренда возвращает сообщение в оборот — иначе оно зависло бы навсегда.
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status);
    }

    [Test]
    public async Task Process_ActiveLeaseOfAnotherHandler_MessageIsNotFetched()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(),
            new InboxMessageIdentity("message-1", "consumer-1"));

        using (var lockScope = sp.CreateScope())
        {
            var lockDb = lockScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await lockDb.Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LockId, Guid.NewGuid())
                    .SetProperty(x => x.LockExpirationTimeUtc, DateTime.UtcNow.AddMinutes(5)));
        }

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(0, processed, "message under an active lease must not be taken by another handler");
    }
}
