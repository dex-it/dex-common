using System;
using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.Outbox.Options;

[SuppressMessage("Performance", "CA1815:Переопределите операторы Equals и равенства для типов значений")]
public readonly struct OutboxRetryStrategyOptions
{
    public DateTime? StartDate { get; }
    public int? CurrentRetry { get; }

    public OutboxRetryStrategyOptions(DateTime? startDate, int? currentRetry = null)
    {
        StartDate = startDate;
        CurrentRetry = currentRetry;
    }
}