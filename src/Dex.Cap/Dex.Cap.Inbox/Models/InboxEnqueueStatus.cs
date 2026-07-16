namespace Dex.Cap.Inbox.Models;

/// <summary>
/// Результат приёма сообщения в инбокс.
/// </summary>
public enum InboxEnqueueStatus
{
    /// <summary>
    /// Сообщение принято и будет обработано.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// Сообщение с такой парой MessageId + ConsumerId уже принято ранее и повторно не сохраняется.
    /// </summary>
    /// <remarks>
    /// Это штатный исход при at-least-once доставке, а не ошибка: источнику следует подтвердить сообщение.
    /// </remarks>
    Duplicate = 1
}