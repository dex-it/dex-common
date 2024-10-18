using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Options;

/// <summary>
/// Audit events options.
/// </summary>
public class AuditEventOptions
{
    /// <summary>
    /// The minimum level of severity of the event for recording.
    /// </summary>
    public AuditEventSeverityLevel MinSeverityLevel { get; set; }

    /// <summary>
    /// The name of the event initiator's system.
    /// </summary>
    public required string SystemName { get; set; }
}
