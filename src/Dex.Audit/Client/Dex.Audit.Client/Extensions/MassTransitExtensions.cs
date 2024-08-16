using Dex.Audit.Client.Abstractions.Messages;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace Dex.Audit.Client.Extensions;

/// <summary>
/// Расширения MassTransit для клиента.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Добавить точку отправления сообщения <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationContext"><see cref="IBusRegistrationContext"/></param>
    public static void AddAuditClientSendEndpoint(this IBusRegistrationContext busRegistrationContext)
    {
        busRegistrationContext.RegisterSendEndPoint<AuditEventMessage>();
    }
}