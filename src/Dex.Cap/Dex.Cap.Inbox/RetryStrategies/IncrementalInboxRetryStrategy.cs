using System;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Повтор с фиксированным сдвигом от предыдущей попытки.
/// </summary>
internal sealed class IncrementalInboxRetryStrategy(TimeSpan interval) : IInboxRetryStrategy
{
    public DateTime CalculateNextStartDate(InboxRetryStrategyOptions options)
    {
        return options.StartDate.Add(interval);
    }
}
