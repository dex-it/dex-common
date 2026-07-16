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
    /// <param name="options">Момент отказа и номер уже сделанной попытки.</param>
    DateTime CalculateNextStartDate(InboxRetryStrategyOptions options);
}