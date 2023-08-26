using System;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxRetryStrategy
    {
        DateTime CalculateNextStartDate(OutboxRetryStrategyOptions? options = default);
    }
}