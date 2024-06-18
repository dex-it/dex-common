using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Options;

/// <summary>
/// Опции системы аудита
/// </summary>
public class AuditEventOptions
{
    /// <summary>
    /// Минимальный уровень важности события для записи
    /// </summary>
    public AuditEventSeverityLevel MinSeverityLevel { get; set; }

    /// <summary>
    /// Название системы инициатора события
    /// </summary>
    public string SystemName { get; set; }
}
