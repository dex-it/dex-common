using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Abstractions.Dtos;

public class AuditSettingsDto
{
    /// <summary>
    /// Идентификатор настройки события.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Тип события.
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Уровень важности события.
    /// </summary>
    public AuditEventSeverityLevel SeverityLevel { get; set; }
}