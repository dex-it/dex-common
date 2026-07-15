using System;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.Interfaces;

public interface IInboxRetryStrategy
{
    DateTime CalculateNextStartDate(InboxRetryStrategyOptions? options = default);
}
