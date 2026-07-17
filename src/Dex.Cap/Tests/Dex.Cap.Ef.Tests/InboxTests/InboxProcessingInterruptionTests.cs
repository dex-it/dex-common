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
/// Прерывания обработки, отличные от истечения аренды: штатная остановка хоста, потеря аренды на пути
/// НЕУДАЧИ и тело, которое десериализуется в null. Ни одно из них не должно портить сообщение или тратить
/// попытку по чужой вине.
/// </summary>
public class InboxProcessingInterruptionTests : BaseTest
{
    /// <summary>
    /// Хост остановлен посреди работы обработчика. Это не отказ сообщения и не истечение аренды: гасится
    /// ВНЕШНИЙ токен, а не lock-токен. Сообщение обязано остаться нетронутым, а отмена дойти наверх.
    /// </summary>
    [Test]
    public async Task Process_HostStopsWhileHandlerRuns_MessageIsUntouchedAndCancellationPropagates()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("message-1", "consumer-1"));

        var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var hostStopping = new CancellationTokenSource();

        // Обработчик уважает переданный токен и ждёт на нём. Гасим внешний токен (остановка хоста), а не
        // lock-токен: именно этим кейс отличается от истечения аренды, покрытого InboxBatchDrainTests.
        TestInboxCommandHandler.OnProcessWithTokenAsync = async (_, token) =>
        {
            handlerEntered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        };

        var processing = sp.GetRequiredService<IInboxHandler>().ProcessAsync(hostStopping.Token);

        await handlerEntered.Task;
        await hostStopping.CancelAsync();

        Assert.CatchAsync<OperationCanceledException>((Func<Task>)(async () => await processing),
            "host stop during processing must propagate cancellation, not swallow it");

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();

        // Транзакция обработчика откатилась, исход не записан: остановка хоста не отказ.
        Assert.AreEqual(InboxMessageStatus.New, envelope.Status, "host stop must not change the message status");
        Assert.AreEqual(0, envelope.Retries, "host stop must not spend an attempt");
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set for the next run");
    }

    /// <summary>
    /// Обработчик падает, но аренда к этому моменту уже потеряна. Отличие от потери аренды на пути УСПЕХА:
    /// там эффект откатывается броском, здесь эффекта нет, ронять нечего, но и записать неудачу нельзя -
    /// строкой уже владеет другой обработчик. Значит, попытка не тратится и статус не меняется.
    /// </summary>
    [Test]
    public async Task Process_HandlerThrowsButLeaseIsAlreadyLost_FailureIsNotRecordedAndAttemptIsNotSpent()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("message-1", "consumer-1"));

        // Обработчик крадёт собственную аренду другим соединением (вне своей транзакции) и затем падает.
        // Путь неудачи попробует записать исход, но предикат владения арендой уже не сойдётся (affected == 0).
        TestInboxCommandHandler.OnProcessAsync = async _ =>
        {
            using var thief = sp.CreateScope();
            await thief.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockId, Guid.NewGuid()));

            throw new InvalidOperationException("handler fails after the lease was stolen");
        };

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();

        Assert.AreEqual(InboxMessageStatus.New, envelope.Status, "a failure that could not be committed must not be written");
        Assert.AreEqual(0, envelope.Retries, "a failure written under a lost lease must not spend an attempt");
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set for the new owner");
    }

    /// <summary>
    /// Тело - валидный JSON-литерал null: десериализация НЕ бросает, а возвращает null. Это отдельная от
    /// битого тела ветка (там JsonException), и инбокс обязан обойтись с ней как с обычным отказом.
    /// </summary>
    [Test]
    public async Task Process_PayloadDeserializesToNull_IsHandledAsAnOrdinaryFailure()
    {
        var sp = InitInboxServiceCollection(retries: 3)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "valid" }, new InboxMessageIdentity("message-1", "consumer-1"));

        using (var breakScope = sp.CreateScope())
        {
            await breakScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Content, "null"));
        }

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();

        Assert.AreEqual(InboxMessageStatus.Failed, envelope.Status, "a null payload is a retriable failure, not a crash");
        Assert.AreEqual(1, envelope.Retries);
        Assert.IsNotNull(envelope.ScheduledStartIndexing, "the message must stay in the fetch set for a retry");
        Assert.IsTrue(envelope.Error!.Contains("null after deserialization", StringComparison.Ordinal),
            $"the outcome must record the null-payload failure, but was: {envelope.Error}");
    }
}
