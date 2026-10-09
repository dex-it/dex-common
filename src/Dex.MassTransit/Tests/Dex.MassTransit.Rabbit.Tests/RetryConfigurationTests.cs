using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.MassTransit.Rabbit.Tests;

/// <summary>
/// Повторы консьюмера через <see cref="MassTransitConfigurationExtensions"/> на конвейере MassTransit.
/// </summary>
/// <remarks>
/// Отмену изнутри консьюмера MassTransit подменяет на <see cref="ConsumerCanceledException"/> до фильтров повтора,
/// поэтому проверка идёт через шину, а не вызовом политики напрямую.
/// </remarks>
[TestFixture]
public class RetryConfigurationTests
{
    private static readonly RetryExponentialIntervals FastRetry = new(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(1));

    [Test]
    public async Task Retry_WhenConsumerIsCanceledAndPolicyAcceptsCancellation_RetriesUpToLimit()
    {
        var calls = await ConsumeUntilFault(
            HttpClientTimeout(),
            c => c.UseRetryConfiguration(ex => ex is OperationCanceledException, retryLimit: 2, FastRetry));

        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public async Task Retry_WhenConsumerIsCanceledAndPolicyRejectsCancellation_DoesNotRetry()
    {
        var calls = await ConsumeUntilFault(
            HttpClientTimeout(),
            c => c.UseRetryConfiguration(ex => ex is TimeoutException, retryLimit: 2, FastRetry));

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Retry_WhenConsumerIsCanceled_PolicyGetsSubstitutedExceptionAsInner()
    {
        var seen = new ConcurrentQueue<Exception>();

        await ConsumeUntilFault(
            HttpClientTimeout(),
            c => c.UseRetryConfiguration(ex => Record(seen, ex), retryLimit: 2, FastRetry));

        Assert.That(seen, Has.None.TypeOf<ConsumerCanceledException>());
        Assert.That(seen, Has.Some.Matches<Exception>(ex => ex.GetType() == typeof(OperationCanceledException) && ex.InnerException is ConsumerCanceledException));
    }

    [Test]
    public async Task Retry_WhenRequestTimesOutAndPolicyAcceptsTimeout_RetriesUpToLimit()
    {
        var calls = await ConsumeUntilFault(
            new RequestTimeoutException(Guid.NewGuid().ToString()),
            c => c.UseRetryConfiguration(ex => ex is TimeoutException, retryLimit: 2, FastRetry));

        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public async Task Retry_WhenExceptionIsPermanent_DoesNotRetry()
    {
        var calls = await ConsumeUntilFault(
            new ArgumentException("permanent"),
            c => c.UseRetryConfiguration(ex => ex is OperationCanceledException or TimeoutException, retryLimit: 2, FastRetry));

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Redelivery_WhenConsumerIsCanceledAndPolicyAcceptsCancellation_RedeliversAfterRetries()
    {
        var calls = await ConsumeUntilFault(
            HttpClientTimeout(),
            c => c.UseRedeliveryRetryConfiguration(
                ex => ex is OperationCanceledException,
                retryLimit: 1,
                FastRetry,
                redeliveryIntervals: [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10)]));

        // (1 попытка + 1 повтор) × (доставка + 2 повторные доставки)
        Assert.That(calls, Is.EqualTo(6));
    }

    /// <remarks>
    /// Как у <c>HttpClient</c> по таймауту: свой отменённый токен и <see cref="TimeoutException"/> внутри.
    /// </remarks>
    private static TaskCanceledException HttpClientTimeout()
        => new("The request was canceled due to the configured HttpClient.Timeout", new TimeoutException(), new CancellationToken(canceled: true));

    private static bool Record(ConcurrentQueue<Exception> seen, Exception exception)
    {
        seen.Enqueue(exception);

        return false;
    }

    private static async Task<int> ConsumeUntilFault(Exception failure, Action<IConsumerConfigurator<FailingConsumer>> configure)
    {
        var state = new ConsumerState(failure);

        await using var provider = new ServiceCollection()
            .AddSingleton(state)
            .AddMassTransitTestHarness(x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddConsumer<FailingConsumer>((_, c) => configure(c));
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(10);
        await harness.Start();

        await harness.Bus.Publish(new RetryTestMessage());

        Assert.That(await harness.Published.Any<Fault<RetryTestMessage>>(), Is.True, "Fault<T> не опубликован");

        return state.Calls;
    }

    [UsedImplicitly]
    public sealed class RetryTestMessage;

    private sealed class ConsumerState(Exception failure)
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Exception Fail()
        {
            Interlocked.Increment(ref _calls);

            return failure;
        }
    }

    [UsedImplicitly]
    private sealed class FailingConsumer(ConsumerState state) : IConsumer<RetryTestMessage>
    {
        public Task Consume(ConsumeContext<RetryTestMessage> context) => throw state.Fail();
    }
}
