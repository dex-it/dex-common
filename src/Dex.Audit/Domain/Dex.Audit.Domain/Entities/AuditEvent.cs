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
    public Guid Id { get; init; }

    /// <summary>
    /// Тип события.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Информация об источнике события.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// Информация о получателе объекта события.
    /// </summary>
    public required Destination Destination { get; init; }

    /// <summary>
    /// Объект события.
    /// </summary>
    public required string EventObject { get; init; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Результат (успех/отказ).
    /// </summary>
    public bool IsSuccess { get; init; }
}
