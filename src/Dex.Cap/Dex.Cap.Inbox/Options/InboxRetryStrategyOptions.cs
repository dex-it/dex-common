using System;

namespace Dex.Cap.Inbox.Options;

public readonly record struct InboxRetryStrategyOptions(DateTime? StartDate, int? CurrentRetry = null)
{
    public DateTime? StartDate { get; } = StartDate;
    public int? CurrentRetry { get; } = CurrentRetry;
}
