using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using MassTransit;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Interface implementation <see cref="IAuditOutputProvider"/>, which publishes audit events through RabbitMQ.
/// </summary>
/// <param name="sendEndpoint">The endpoint for publishing messages.</param>
internal sealed class AuditOutputProvider(ISendEndpointProvider sendEndpoint)
    : IAuditOutputProvider
{
    /// <summary>
    /// Publishes an audit event via RabbitMQ.
    /// </summary>
    /// <param name="auditEvent">Audit event to be published.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task PublishEventAsync(
        AuditEventMessage auditEvent,
        CancellationToken cancellationToken = default)
    {
        return sendEndpoint
            .Send(auditEvent, cancellationToken);
    }
}
