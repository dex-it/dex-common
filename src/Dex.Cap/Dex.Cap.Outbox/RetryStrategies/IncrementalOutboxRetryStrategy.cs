using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.RetryStrategies;

internal sealed class IncrementalOutboxRetryStrategy : IOutboxRetryStrategy
{
    private readonly TimeSpan _interval;

    public IncrementalOutboxRetryStrategy(TimeSpan interval)
    {
        _interval = interval;
    }

    public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options)
    {
        var startDate = options?.StartDate ?? DateTime.UtcNow;
        return startDate.Add(_interval);
    }
}