using System;
using Dex.Extensions;
using MassTransit;

namespace Dex.MassTransit.Rabbit;

public static class MassTransitConfigurationExtensions
{
    private const int DefaultRetryLimit = 5;

    /// <summary>
    /// Конфигурация с настроенными Redelivery и Retry
    /// <remarks> Используйте TransientExceptionsHandler для перехвата временных ошибок </remarks>
    /// </summary>
    public static IConsumerConfigurator UseRedeliveryRetryConfiguration<TConsumer>(
        this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException,
        int? retryLimit = null)
        where TConsumer : class
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));

        // важна последовательность usage (вначале UseDelayedRedelivery, затем UseMessageRetry)
        configurator.UseDelayedRedelivery(redeliveryConfigurator =>
        {
            redeliveryConfigurator.Intervals(5.Minutes(), 15.Minutes(), 30.Minutes(), 1.Hours(), 3.Hours(), 6.Hours());
            redeliveryConfigurator.Handle(checkTransientException);
        });

        configurator.UseRetryConfiguration(checkTransientException, retryLimit);

        return configurator;
    }

    /// <summary>
    /// Конфигурация Retry
    /// <remarks> Используйте TransientExceptionsHandler для перехвата временных ошибок </remarks>
    /// </summary>
    public static IConsumerConfigurator UseRetryConfiguration<TConsumer>(
        this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException,
        int? retryLimit = null)
        where TConsumer : class
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));

        configurator.UseMessageRetry(retryConfigurator =>
        {
            retryLimit ??= DefaultRetryLimit;

            retryConfigurator.Incremental(retryLimit.Value, 1.Seconds(), 1.Seconds());
            retryConfigurator.Handle(checkTransientException);
        });

        return configurator;
    }

    /// <summary>
    /// Не использовать дефолтную настройку эндпоинта (concurrencyLimit = 1 и prefetchCount = 1) совместно с настройкой консьюмера с Redelivery,
    /// так как не будет гарантирована последовательная обработка сообщений (сообщение удалится из очереди и консьюмер получит следующее)
    /// </summary>
    public static IEndpointConfigurator UseLimitPrefetchConfiguration(
        this IEndpointConfigurator configurator,
        int concurrencyLimit = 1,
        int prefetchCount = 1)
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));
        if (concurrencyLimit >= 100) throw new ArgumentOutOfRangeException(nameof(concurrencyLimit));

        configurator.UseConcurrencyLimit(concurrencyLimit);
        configurator.ConfigureTransport(transportConfigurator => transportConfigurator.PrefetchCount = prefetchCount);

        return configurator;
    }
}