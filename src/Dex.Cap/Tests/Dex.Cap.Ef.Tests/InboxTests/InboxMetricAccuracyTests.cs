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
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Счётчики обязаны считать ФАКТ, а не намерение.
/// </summary>
public class InboxMetricAccuracyTests : BaseTest
{
    private bool _retryStrategyBeforeTest;

    /// <remarks>
    /// Снимок, а не константа: <see cref="TestDbContext.IsRetryStrategy"/> это статическое состояние всей
    /// сборки, и вернуть его надо ровно тем, каким оно было. Восстановление «в false» тихо ломало бы соседние
    /// фикстуры, которым стратегия повторов нужна включённой.
    /// </remarks>
    [SetUp]
    public void CaptureRetryStrategy() => _retryStrategyBeforeTest = TestDbContext.IsRetryStrategy;

    [TearDown]
    public void RestoreDbContextState()
    {
        TestDbContext.Interceptor = null;
        TestDbContext.IsRetryStrategy = _retryStrategyBeforeTest;
    }

    /// <summary>
    /// Транзиентный отказ фиксации переигрывает блок обработчика целиком. Сообщение при этом
    /// закоммичено ровно один раз, поэтому и успех обязан быть засчитан один раз.
    /// </summary>
    [Test]
    public async Task Process_TransientCommitFailureReplaysTheJob_CountsSuccessOnce()
    {
        var interceptor = new CommitFailsOnceWhenArmedInterceptor();
        var metrics = new CountingInboxMetricCollector();

        TestDbContext.IsRetryStrategy = true;
        TestDbContext.Interceptor = interceptor;

        // Взводим отказ ИЗ обработчика: так падает именно транзакция задачи, а не захват партии.
        void Arm(object? sender, TestInboxCommand message) => interceptor.Arm();
        TestInboxCommandHandler.OnProcess += Arm;

        try
        {
            var services = InitInboxServiceCollection()
                .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>();

            services.RemoveAll<IInboxMetricCollector>();
            services.AddSingleton<IInboxMetricCollector>(metrics);

            var sp = services.BuildServiceProvider();

            await sp.GetRequiredService<IInboxService>()
                .EnqueueAsync(new TestInboxCommand { Args = "x" }, new InboxMessageIdentity("m-1", "c-1"));

            await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

            Assert.AreEqual(1, interceptor.FiredCount, "the transient failure must have been injected exactly once");

            var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
            Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status, "the message must end up committed as succeeded");

            Assert.AreEqual(1, metrics.ProcessJobSuccessCount,
                "one committed message must count as exactly one success, no matter how many attempts the retry strategy made");
        }
        finally
        {
            TestInboxCommandHandler.OnProcess -= Arm;
        }
    }

    /// <summary>
    /// Партия целиком из непригодных строк это состоявшийся цикл: захват был, строки похоронены.
    /// Штамп живости обязан сдвинуться, иначе health check отдаёт Degraded при живом обработчике.
    /// </summary>
    [Test]
    public async Task Process_BatchOfPoisonedRowsOnly_AdvancesTheLivenessStamp()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>()
            .EnqueueAsync(new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));

        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        var statistic = sp.GetRequiredService<IInboxStatistic>();
        var before = statistic.GetLastStamp();

        await Task.Delay(20);

        Assert.AreEqual(1, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None),
            "the claim happened, so the cycle is not empty");

        Assert.Greater(statistic.GetLastStamp(), before,
            "a cycle that claimed and dead lettered rows is a sign of life and must advance the stamp");
    }

    /// <summary>
    /// Обратная сторона: цикл, не доехавший до БД, живым НЕ считается, иначе health check
    /// никогда не заметит недоступность хранилища.
    /// </summary>
    [Test]
    public async Task Process_FetchFails_DoesNotAdvanceTheLivenessStamp()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        // Роняем хранилище под обработчиком: таблицы больше нет, выборка обязана упасть.
        await GetDb(sp).Database.EnsureDeletedAsync();

        var statistic = sp.GetRequiredService<IInboxStatistic>();
        var before = statistic.GetLastStamp();

        await Task.Delay(20);

        NUnit.Framework.Assert.CatchAsync((Func<Task>)(async () =>
            await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None)));

        Assert.AreEqual(before, statistic.GetLastStamp(),
            "a cycle that never reached the storage is not a sign of life: the health check must be able to see the outage");
    }

    /// <summary>
    /// Роняет транзиентной ошибкой первую фиксацию, случившуюся после взвода.
    /// </summary>
    /// <remarks>
    /// Взвод нужен, чтобы попасть именно в транзакцию задачи: до неё коммитится ещё и захват партии.
    /// <see cref="TimeoutException"/> обе стороны (стратегия Npgsql и фильтр Common.Ef) трактуют как транзиентный.
    /// </remarks>
    private sealed class CommitFailsOnceWhenArmedInterceptor : DbTransactionInterceptor
    {
        private int _armed;
        private int _fired;

        public int FiredCount => Volatile.Read(ref _fired);

        public void Arm() => Interlocked.Exchange(ref _armed, 1);

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _armed, 0, 1) == 1 && Interlocked.CompareExchange(ref _fired, 1, 0) == 0)
            {
                throw new TimeoutException("Simulated transient failure of the job transaction commit");
            }

            return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }
    }

    /// <summary>
    /// Считает вызовы, ничего не публикуя: тесту нужен факт вызова, а не Meter.
    /// </summary>
    private sealed class CountingInboxMetricCollector : IInboxMetricCollector
    {
        private int _processJobSuccessCount;

        public int ProcessJobSuccessCount => Volatile.Read(ref _processJobSuccessCount);

        public void IncProcessCount()
        {
        }

        public void IncEmptyProcessCount()
        {
        }

        public void IncProcessJobCount()
        {
        }

        public void IncProcessJobSuccessCount() => Interlocked.Increment(ref _processJobSuccessCount);

        public void IncProcessJobFailedCount()
        {
        }

        public void IncDeadLetteredCount()
        {
        }

        public void IncDuplicateCount()
        {
        }

        public void IncExpiredBeforeStartCount()
        {
        }

        public void IncLeaseLostCount()
        {
        }

        public void AddProcessJobDuration(TimeSpan duration)
        {
        }

        public DateTime GetLastStamp() => DateTime.UtcNow;
    }
}