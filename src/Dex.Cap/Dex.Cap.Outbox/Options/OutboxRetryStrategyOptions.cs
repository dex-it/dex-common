using System;

namespace Dex.Cap.Outbox.Options;

public readonly record struct OutboxRetryStrategyOptions(DateTime? StartDate, int? CurrentRetry = null)
{
    public DateTime? StartDate { get; } = StartDate;
    public int? CurrentRetry { get; } = CurrentRetry;
}