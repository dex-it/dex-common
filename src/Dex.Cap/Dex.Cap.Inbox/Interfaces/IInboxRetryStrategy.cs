using System;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Когда повторить сообщение, обработка которого завершилась ошибкой.
/// </summary>
public interface IInboxRetryStrategy
{
    /// <summary>
    /// Вычислить момент следующей попытки.
    /// </summary>
    DateTime CalculateNextStartDate(InboxRetryStrategyOptions? options = default);
}
