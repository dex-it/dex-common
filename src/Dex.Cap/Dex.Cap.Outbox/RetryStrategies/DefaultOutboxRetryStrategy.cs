using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.RetryStrategies
{
    internal sealed class DefaultOutboxRetryStrategy : IOutboxRetryStrategy
    {
        public DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options)
        {
            return options?.StartDate ?? DateTime.UtcNow;
        }
    }
}