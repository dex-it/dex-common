namespace Dex.Audit.Client.Messages;

/// <summary>
/// Базовая информация о событии аудита
/// </summary>
public class AuditEventBaseInfo
{
    /// <summary>
    /// Тип события аудита
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Объект сообщения аудита
    /// </summary>
    public string? EventObject { get; }

    /// <summary>
    /// Текст сообщения о событии
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Результат (успех/отказ) действия
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditEventBaseInfo"/> 
    /// </summary>
    /// <param name="eventType">Тип события аудита</param>
    /// <param name="eventObject">Объект события аудита</param>
    /// <param name="message">Текст сообщения о событии</param>
    /// <param name="isSuccess">Результат (успех/отказ) действия</param>
    public AuditEventBaseInfo(string eventType, string? eventObject, string? message, bool isSuccess)
    {
        EventType = eventType;
        EventObject = eventObject;
        Message = message;
        IsSuccess = isSuccess;
    }
}
