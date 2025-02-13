using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.RetryStrategies;

internal sealed class DefaultOutboxRetryStrategy : IOutboxRetryStrategy
{
    public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options, int? maxRetriesCount = null)
    {
        return options?.StartDate ?? DateTime.UtcNow;
    }

    public void ResetRetriesIfNeeded(int maxRetriesCount, IOutboxLockedJob outboxLockedJob)
    {
    }
}