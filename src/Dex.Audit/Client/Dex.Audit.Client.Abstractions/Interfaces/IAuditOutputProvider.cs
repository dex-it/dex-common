using Dex.Audit.Client.Abstractions.Messages;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Defines a method for publishing audit events.
/// </summary>
internal interface IAuditOutputProvider
{
    /// <summary>
    /// Asynchronously publishes an audit event.
    /// </summary>
    /// <param name="auditEvent">Audit event to be published.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task PublishEventAsync(
        AuditEventMessage auditEvent,
        CancellationToken cancellationToken = default);
}
