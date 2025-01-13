using System.ComponentModel;

namespace Dex.Audit.Domain.Enums;

/// <summary>
/// The level of severity of the audit event.
/// </summary>
public enum AuditEventSeverityLevel
{
    [Description("Low level.")]
    Low = 0,

    [Description("Medium level.")]
    Medium = 1,

    [Description("High level.")]
    High = 2,

    [Description("Critical level.")]
    Critical = 3
}
