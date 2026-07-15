using System;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox.RetryStrategies;

public sealed class InboxRetryStrategyConfigurator
{
    private IInboxRetryStrategy _retryStrategy = new DefaultInboxRetryStrategy();

    public IInboxRetryStrategy RetryStrategy
    {
        get => _retryStrategy;
        set => _retryStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Повторять без задержки.
    /// </summary>
    public void UseDefaultStrategy() => RetryStrategy = new DefaultInboxRetryStrategy();

    /// <summary>
    /// Повторять с фиксированным интервалом.
    /// </summary>
    public void UseIncrementalStrategy(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        RetryStrategy = new IncrementalInboxRetryStrategy(interval);
    }

    /// <summary>
    /// Повторять с экспоненциальной задержкой, ограниченной сверху.
    /// </summary>
    public void UseExponentialStrategy(TimeSpan baseDelay, TimeSpan maxDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baseDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, baseDelay);
        RetryStrategy = new ExponentialInboxRetryStrategy(baseDelay, maxDelay);
    }
}
