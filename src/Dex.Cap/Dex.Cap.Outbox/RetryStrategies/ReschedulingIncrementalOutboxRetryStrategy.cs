using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.RetryStrategies;

public class ReschedulingIncrementalOutboxRetryStrategy : IOutboxRetryStrategy
{
    private readonly TimeSpan _interval;
    private readonly TimeSpan _rescheduleInterval;

    public ReschedulingIncrementalOutboxRetryStrategy(TimeSpan interval, TimeSpan rescheduleInterval)
    {
        _interval = interval;
        _rescheduleInterval = rescheduleInterval;
    }

    public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options = default, int? maxRetriesCount = null)
    {
        if (!options.HasValue)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!maxRetriesCount.HasValue)
        {
            throw new ArgumentNullException(nameof(maxRetriesCount));
        }

        var startDate = options.Value.StartDate ?? DateTime.UtcNow;

        if (options.Value.CurrentRetry == maxRetriesCount)
        {
            return startDate.Add(_rescheduleInterval);
        }

        return startDate.Add(_interval);
    }

    public void ResetRetriesIfNeeded(int maxRetriesCount, IOutboxLockedJob outboxLockedJob)
    {
        if (outboxLockedJob == null)
        {
            throw new ArgumentNullException(nameof(outboxLockedJob));
        }

        if (outboxLockedJob.Envelope.Retries == maxRetriesCount)
        {
            outboxLockedJob.Envelope.Retries = 0;
        }
    }
}