using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Domain.Models;

/// <summary>
/// Настройки событий аудита.
/// </summary>
public class AuditSettings
{
    /// <summary>
    /// Идентификатор настройки события.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип события.
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Уровень важности события.
    /// </summary>
    public AuditEventSeverityLevel SeverityLevel { get; set; }
}
