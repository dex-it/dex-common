using System;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxRetryStrategy
{
    DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options = default, int? maxRetriesCount = null);

    void ResetRetriesIfNeeded(int maxRetriesCount, IOutboxLockedJob outboxLockedJob);
}