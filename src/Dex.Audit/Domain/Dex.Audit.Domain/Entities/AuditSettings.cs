using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Domain.Entities;

/// <summary>
/// Audit event settings.
/// </summary>
public sealed class AuditSettings
{
    /// <summary>
    /// Event setting identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Event type.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Event severity level.
    /// </summary>
    public AuditEventSeverityLevel SeverityLevel { get; set; }
}