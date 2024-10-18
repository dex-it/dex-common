using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Domain.Entities;

/// <summary>
/// Настройки событий аудита.
/// </summary>
public class AuditSettings
{
    /// <summary>
    /// Идентификатор настройки события.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Тип события.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Уровень важности события.
    /// </summary>
    public AuditEventSeverityLevel SeverityLevel { get; set; }
}
