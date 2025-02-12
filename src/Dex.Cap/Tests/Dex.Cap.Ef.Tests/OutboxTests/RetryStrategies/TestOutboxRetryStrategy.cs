using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;

internal sealed class TestOutboxRetryStrategy : IOutboxRetryStrategy
{
    private readonly TimeSpan _interval;

    public TestOutboxRetryStrategy(TimeSpan interval)
    {
        _interval = interval;
    }

    public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options, int? maxRetriesCount = null)
    {
        var startDate = options?.StartDate ?? DateTime.UtcNow;
        return startDate.Add((options?.CurrentRetry ?? 1) * _interval);
    }

    public void ResetRetriesIfNeeded(int maxRetriesCount, IOutboxLockedJob outboxLockedJob)
    {
    }
}