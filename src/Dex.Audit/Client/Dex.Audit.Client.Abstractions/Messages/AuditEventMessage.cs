using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Abstractions.Messages;

/// <summary>
/// Contract for reporting an audit event.
/// </summary>
public class AuditEventMessage
{
    /// <summary>
    /// The name of the AC source of information about the event.
    /// </summary>
    public string? DeviceVendor { get; init; }

    /// <summary>
    /// The version of the AC source of information about the event.
    /// </summary>
    public string? DeviceVersion { get; init; }

    /// <summary>
    /// The name of the AC audit log, the source of information about the event.
    /// </summary>
    public string? DeviceProduct { get; init; }

    /// <summary>
    /// The system name (login) of the user who initiated the event.
    /// </summary>
    public string? SourceUser { get; init; }

    /// <summary>
    /// The domain (or workgroup name) of the user who initiated the event.
    /// </summary>
    public string? SourceUserDomain { get; init; }

    /// <summary>
    /// The IP address of the event source host.
    /// </summary>
    public string? SourceIpAddress { get; init; }

    /// <summary>
    /// MAC address of the event source host.
    /// </summary>
    public string? SourceMacAddress { get; init; }

    /// <summary>
    /// DNS name of the event source.
    /// </summary>
    public string? SourceDnsName { get; init; }

    /// <summary>
    /// Netbios name or hostname of the event initiator host.
    /// </summary>
    public string? SourceHost { get; init; }

    /// <summary>
    /// The system name (login) of the recipient's user.
    /// </summary>
    public string? DestinationUser { get; set; }

    /// <summary>
    /// The domain (or workgroup name) of the recipient's user.
    /// </summary>
    public string? DestinationDomain { get; set; }

    /// <summary>
    /// The IP address of the receiving host.
    /// </summary>
    public string? DestinationIpAddress { get; set; }

    /// <summary>
    /// MAC address of the receiving host.
    /// </summary>
    public string? DestinationMacAddress { get; set; }

    /// <summary>
    /// The DNS name of the recipient.
    /// </summary>
    public string? DestinationDnsName { get; set; }

    /// <summary>
    /// Netbios name or hostname of the recipient's host.
    /// </summary>
    public string? DestinationHost { get; set; }

    /// <summary>
    /// The system ID of the event message.
    /// </summary>
    public long? DeviceEventClassId { get; init; }

    /// <summary>
    /// The port is on the source side.
    /// </summary>
    public string? SourcePort { get; init; }

    /// <summary>
    /// The port is on the recipient's side.
    /// </summary>
    public string? DestinationPort { get; set; }

    /// <summary>
    /// The protocol is on the source side.
    /// </summary>
    public string? SourceProtocol { get; init; }

    /// <summary>
    /// The system time of the event source.
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    /// The recipient's system time.
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// The GMT time of the event source.
    /// </summary>
    public DateTime SourceGmtDate { get; init; }

    /// <summary>
    /// The recipient's GMT time.
    /// </summary>
    public DateTime DestinationGmtDate { get; set; }

    /// <summary>
    /// The event object.
    /// </summary>
    public string? EventObject { get; init; }

    /// <summary>
    /// The text of the message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// The full name of the process (service).
    /// </summary>
    public string? DeviceProcessName { get; init; }

    /// <summary>
    /// The result (success/failure).
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The type of audit event.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The minimum level of severity of the event for recording, specified on the side of the event source.
    /// </summary>
    public AuditEventSeverityLevel SourceMinSeverityLevel { get; set; }

    /// <summary>
    /// The ID of the audit setting.
    /// </summary>
    public Guid? AuditSettingsId { get; set; }
}
