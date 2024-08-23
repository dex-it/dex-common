using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Server.Consumers;
using Dex.Extensions;
using MassTransit;

namespace Dex.Audit.Server.Extensions;

/// <summary>
/// Расширения MassTransit для клиента.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Добавить точку получения сообщения <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationConfigurator"><see cref="IBusRegistrationConfigurator"/></param>
    public static void AddAuditServerConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AuditEventConsumer>();
    }

    /// <summary>
    /// Добавить точку получения сообщений <see cref="AuditEventMessage"/>.
    /// </summary>
    /// <param name="busRegistrationContext"><see cref="IBusRegistrationContext"/></param>
    /// <param name="context"><see cref="IBusRegistrationContext"/></param>
    /// <param name="enableRetry">Включить ли повторные попытки получить сообщение.</param>
    /// <param name="prefetchCount">Количество предварительной выборки.</param>
    /// <param name="messageLimit">Лимит сообщений.</param>
    /// <param name="timeLimitSeconds">Время ожидания сообщений в секундах.</param>
    /// <param name="concurrencyLimit">Количество одновременно принмаемых сообщений.</param>
    /// <param name="retryCount">Количество повторных попыток.</param>
    /// <param name="retryIntervalSeconds">Интервал повторных попыток.</param>
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