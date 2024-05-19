using System;
using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.Outbox.Options;

[SuppressMessage("Performance", "CA1815:Переопределите операторы Equals и равенства для типов значений")]
public readonly struct OutboxRetryStrategyOptions(DateTime? startDate, int? currentRetry = null)
{
    public DateTime? StartDate { get; } = startDate;
    public int? CurrentRetry { get; } = currentRetry;
}