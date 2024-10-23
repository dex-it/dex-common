using Dex.Audit.Client.Grpc.Consumers;
using Dex.Audit.Implementations.Common.Dto;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace Dex.Audit.Client.Grpc.Extensions;

/// <summary>
/// MassTransit extensions for the client.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Add a consumer for <see cref="AuditSettingsDto"/>.
    /// </summary>
    /// <param name="busRegistrationConfigurator"><see cref="IBusRegistrationConfigurator"/></param>
    public static void AddSimpleAuditClientConsumer(
        this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<SimpleAuditSettingsUpdatedConsumer>();
    }

    /// <summary>
    /// Add a ReceiveEndpoint for <see cref="AuditSettingsDto"/>.
    /// </summary>
    /// <param name="busRegistrationContext"><see cref="IBusRegistrationContext"/></param>
    /// <param name="configurator"><see cref="IRabbitMqBusFactoryConfigurator"/></param>
    public static void AddSimpleAuditClientReceiveEndpoint(
        this IBusRegistrationContext busRegistrationContext,
        IRabbitMqBusFactoryConfigurator configurator)
    {
        busRegistrationContext.RegisterReceiveEndpoint<AuditSettingsDto, SimpleAuditSettingsUpdatedConsumer>(configurator);
    }
}