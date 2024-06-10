namespace Dex.Audit.Domain.Models.AuditEvent;

/// <summary>
/// Событие аудита.
/// </summary>
public class AuditEvent
{
    /// <summary>
    ///  Идентификатор строки о событии ИБ в рамках одного журнала АС.
    /// </summary>
    public long ExternalId { get; set; }

    /// <summary>
    /// Код события в журнале АС.
    /// </summary>
    public string? EventCode { get; set; }

    /// <summary>
    /// Информация об источнике события.
    /// </summary>
    public Source Source { get; set; }

    /// <summary>
    /// Информация о получателе объекта события.
    /// </summary>
    public Destination Destination { get; set; }

    /// <summary>
    /// Объект события 
    /// </summary>
    public string EventObject { get; set; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Результат (успех/отказ).
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Краткое наименование события.
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// Внешний ключ к настройкам события.
    /// </summary>
    public int AuditSettingsId { get; set; }

    /// <summary>
    /// Настройки события аудита.
    /// </summary>
    public AuditSettings AuditSettings { get; set; }
}
