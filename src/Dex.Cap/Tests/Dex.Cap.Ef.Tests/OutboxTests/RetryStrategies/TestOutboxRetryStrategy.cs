using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;

internal sealed class TestOutboxRetryStrategy(TimeSpan interval) : IOutboxRetryStrategy
{
    public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options)
    {
        var startDate = options?.StartDate ?? DateTime.UtcNow;
        return startDate.Add((options?.CurrentRetry ?? 1) * interval);
    }
}