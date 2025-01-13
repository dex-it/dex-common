using Dex.Audit.Client.Abstractions.Messages;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace Dex.Audit.Client.Extensions;

/// <summary>
/// MassTransit extensions for the client.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Add a send endpoint for a message <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationContext"><see cref="IBusRegistrationContext"/></param>
    public static void AddAuditClientSendEndpoint(this IBusRegistrationContext busRegistrationContext)
    {
        busRegistrationContext.RegisterSendEndPoint<AuditEventMessage>();
    }
}