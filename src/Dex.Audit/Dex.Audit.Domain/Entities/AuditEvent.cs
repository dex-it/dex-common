using Dex.Audit.Domain.ValueObjects;

namespace Dex.Audit.Domain.Entities;

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
    public required Source Source { get; set; }

    /// <summary>
    /// Информация о получателе объекта события.
    /// </summary>
    public required Destination Destination { get; set; }

    /// <summary>
    /// Объект события 
    /// </summary>
    public required string EventObject { get; set; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Результат (успех/отказ).
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Краткое наименование события.
    /// </summary>
    public required string EventName { get; set; }
}
