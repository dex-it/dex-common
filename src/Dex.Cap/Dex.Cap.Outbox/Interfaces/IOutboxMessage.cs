namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxMessage
{
    /// <summary>
    /// Уникальный id типа сообщения в аутбоксе
    /// Используется для сопоставления входящих сообщений и принимающими обработчиками
    /// </summary>
    static abstract string OutboxTypeId { get; }

    /// <summary>
    /// Запретить или разрешить участвовать в авто-публикации через PublisherOutboxHandler
    /// <code>services.AddOutboxPublisher();</code>
    /// </summary>
    static virtual bool AllowAutoPublishing => true;
}