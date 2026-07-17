using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Контракт приёма при транзиентном отказе: вставка НЕ повторяется, отказ доходит до вызывающего.
/// </summary>
/// <remarks>
/// Тест сторожит документированное поведение, на которое опирается смысл
/// <see cref="InboxEnqueueStatus.Duplicate"/>. Если приём когда-нибудь обернут в стратегию повторов, тест
/// упадёт, и это правильно: вместе с повтором придётся пересматривать и контракт статуса, потому что повторная
/// вставка находила бы собственную строку и возвращала бы Duplicate за сообщение, принятое этим же вызовом.
/// </remarks>
public class InboxEnqueueRetryContractTests : BaseTest
{
    private bool _retryStrategyBeforeTest;

    [SetUp]
    public void CaptureRetryStrategy() => _retryStrategyBeforeTest = TestDbContext.IsRetryStrategy;

    [TearDown]
    public void RestoreDbContextState()
    {
        TestDbContext.Interceptor = null;
        TestDbContext.IsRetryStrategy = _retryStrategyBeforeTest;
    }

    [Test]
    public async Task Enqueue_TransientFailureAfterTheInsertLanded_SurfacesToTheCallerAndIsNotRetried()
    {
        var interceptor = new FailOnceAfterInboxInsertInterceptor();

        TestDbContext.IsRetryStrategy = true;
        TestDbContext.Interceptor = interceptor;

        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        // Контроль: стратегия повторов включена и эту ошибку она действительно переигрывает.
        // Без него тест доказывал бы лишь то, что повторять было некому.
        var strategy = GetDb(sp).Database.CreateExecutionStrategy();
        Assert.IsTrue(strategy.RetriesOnFailure, "the test is meaningless unless the retrying strategy is on");

        var attemptsUnderStrategy = 0;
        await strategy.ExecuteAsync(async ct =>
        {
            attemptsUnderStrategy++;

            if (attemptsUnderStrategy == 1)
            {
                throw new TimeoutException("control: this very exception must be retryable for the strategy");
            }

            await Task.Yield();
            return 0;
        }, CancellationToken.None);

        Assert.AreEqual(2, attemptsUnderStrategy, "the strategy must treat this exception as transient");

        // Собственно проверка: тот же транзиентный отказ на вставке приёма.
        Assert.CatchAsync<TimeoutException>((Func<Task>)(async () =>
            await sp.GetRequiredService<IInboxService>()
                .EnqueueAsync(new TestInboxCommand { Args = "x" }, new InboxMessageIdentity("m-1", "c-1"))));

        Assert.AreEqual(1, interceptor.InsertAttempts,
            "the insert must not be retried: EF deliberately keeps ExecutionStrategy away from ExecuteSqlRaw, " +
            "and the inbox does not add its own retry, so the source redelivery is the retry");

        // Строка при этом уже лежит: именно поэтому подтверждение источнику отдаётся только после успеха,
        // а отказ обязан дойти до вызывающего.
        Assert.AreEqual(1, await GetDb(sp).Set<InboxEnvelope>().CountAsync(x => x.MessageId == "m-1"));
    }

    /// <summary>
    /// Редоставка того же сообщения это штатный Duplicate, и вставку она не повторяет.
    /// </summary>
    [Test]
    public async Task Enqueue_SameIdentityTwice_ReportsDuplicateWithoutASecondRow()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        var identity = new InboxMessageIdentity("m-1", "c-1");

        Assert.AreEqual(InboxEnqueueStatus.Accepted, await inbox.EnqueueAsync(new TestInboxCommand { Args = "first" }, identity));
        Assert.AreEqual(InboxEnqueueStatus.Duplicate, await inbox.EnqueueAsync(new TestInboxCommand { Args = "second" }, identity));

        Assert.AreEqual(1, await GetDb(sp).Set<InboxEnvelope>().CountAsync(x => x.MessageId == "m-1"));
    }

    /// <summary>
    /// Роняет транзиентной ошибкой первый INSERT инбокса ПОСЛЕ того, как он успешно выполнился:
    /// так выглядит потеря подтверждения уже прошедшей вставки.
    /// </summary>
    private sealed class FailOnceAfterInboxInsertInterceptor : DbCommandInterceptor
    {
        private int _fired;
        private int _attempts;

        public int InsertAttempts => Volatile.Read(ref _attempts);

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("ON CONFLICT", StringComparison.Ordinal))
            {
                return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
            }

            Interlocked.Increment(ref _attempts);

            if (Interlocked.CompareExchange(ref _fired, 1, 0) == 0)
            {
                throw new TimeoutException("Simulated transient failure right after the insert landed");
            }

            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
    }
}
