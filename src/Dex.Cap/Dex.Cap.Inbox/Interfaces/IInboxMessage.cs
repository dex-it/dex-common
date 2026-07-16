namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Сообщение инбокса.
/// </summary>
public interface IInboxMessage
{
    /// <summary>
    /// Уникальный id типа сообщения в инбоксе.
    /// Используется для сопоставления сохранённых сообщений с обработчиками.
    /// </summary>
    /// <remarks>
    /// Значение попадает в хранилище и читается после рестарта и после деплоя,
    /// поэтому менять его у существующего типа нельзя: сохранённые сообщения перестанут резолвиться.
    /// </remarks>
    static abstract string InboxTypeId { get; }
}