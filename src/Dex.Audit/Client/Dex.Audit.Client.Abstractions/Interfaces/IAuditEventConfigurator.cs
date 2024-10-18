using Dex.Audit.Client.Abstractions.Messages;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Interface for configuring audit events.
/// </summary>
public interface IAuditEventConfigurator
{
    /// <summary>
    /// Configure an audit event based on the transmitted basic information about the event.
    /// </summary>
    /// <param name="auditEventBaseInfo">Basic information about the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task<AuditEventMessage> ConfigureAuditEventAsync(
        AuditEventBaseInfo auditEventBaseInfo,
        CancellationToken cancellationToken = default);
}
