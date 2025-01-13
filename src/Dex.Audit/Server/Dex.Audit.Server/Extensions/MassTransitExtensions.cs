using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Server.Consumers;
using Dex.Extensions;
using MassTransit;

namespace Dex.Audit.Server.Extensions;

/// <summary>
/// MassTransit extensions for the audit server client.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Add a consumer for <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationConfigurator"><see cref="IBusRegistrationConfigurator"/></param>
    public static void AddAuditServerConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AuditEventConsumer>();
    }

    /// <summary>
    /// Configures a receive endpoint for <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationContext"><see cref="IRabbitMqBusFactoryConfigurator"/></param>
    /// <param name="context"><see cref="IBusRegistrationContext"/></param>
    /// <param name="enableRetry">Whether to enable retry for message processing.</param>
    /// <param name="prefetchCount">Number of messages to prefetch.</param>
    /// <param name="messageLimit">Limit of messages to batch.</param>
    /// <param name="timeLimitSeconds">Time limit for message processing in seconds.</param>
    /// <param name="concurrencyLimit">Limit for concurrent message processing.</param>
    /// <param name="retryCount">Number of retry attempts.</param>
    /// <param name="retryIntervalSeconds">Interval between retry attempts in seconds.</param>
    public static void AddAuditServerReceiveEndpoint(
        this IRabbitMqBusFactoryConfigurator busRegistrationContext,
        IBusRegistrationContext context,
        bool enableRetry,
        int prefetchCount = 600,
        int messageLimit = 500,
        int timeLimitSeconds = 1,
        int concurrencyLimit = 1,
        int retryCount = 2,
        int retryIntervalSeconds = 1)
    {
        busRegistrationContext.ReceiveEndpoint(nameof(AuditEventMessage), endpointConfigurator =>
        {
            endpointConfigurator.PrefetchCount = prefetchCount;

            endpointConfigurator.Batch<AuditEventMessage>(b =>
            {
                b.MessageLimit = messageLimit;
                b.TimeLimit = TimeSpan.FromSeconds(timeLimitSeconds);

                b.Consumer<AuditEventConsumer, AuditEventMessage>(context);

                b.ConcurrencyLimit = concurrencyLimit;
            });

            if (enableRetry)
            {
                endpointConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.SetRetryPolicy(filter =>
                        filter.Interval(retryCount, retryIntervalSeconds.Seconds())));
            }
        });
    }
}
