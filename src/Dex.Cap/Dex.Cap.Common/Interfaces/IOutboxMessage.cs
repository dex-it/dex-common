namespace Dex.Cap.Common.Interfaces;

/// <summary>
/// Сообщение аутбокса
/// </summary>
public interface IOutboxMessage
{
    /// <summary>
    /// Уникальный id типа сообщения в аутбоксе.
    /// Используется для сопоставления входящих сообщений и принимающими обработчиками
    /// </summary>
    string OutboxTypeId { get; }

    /// <summary>
    /// Запретить или разрешить участвовать в авто-публикации через PublisherOutboxHandler
    /// <code>services.AddOutboxPublisher();</code>
    /// </summary>
    bool AllowAutoPublishing => true;
}