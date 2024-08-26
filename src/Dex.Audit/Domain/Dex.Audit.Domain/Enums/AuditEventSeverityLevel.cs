using System.ComponentModel;

namespace Dex.Audit.Domain.Enums;

/// <summary>
/// Уровень критичности события аудита.
/// </summary>
public enum AuditEventSeverityLevel
{
    [Description("Низкий уровень.")]
    Low = 0,

    [Description("Средний уровень.")]
    Medium = 1,

    [Description("Высокий уровень.")]
    High = 2,

    [Description("Критический уровень.")]
    Critical = 3
}
