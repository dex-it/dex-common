using System;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Повтор без задержки: следующая попытка возможна сразу.
/// </summary>
internal sealed class DefaultInboxRetryStrategy : IInboxRetryStrategy
{
    public DateTime CalculateNextStartDate(InboxRetryStrategyOptions options)
    {
        return options.StartDate;
    }
}