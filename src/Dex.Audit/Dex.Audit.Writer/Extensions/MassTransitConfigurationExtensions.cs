using Dex.Extensions;
using MassTransit;

namespace Dex.Audit.Writer.Extensions;

public static class MassTransitConfigurationExtensions
{
    private const int DefaultRetryLimit = 20;

    public static void UseDefaultConfiguration<TConsumer>(this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException, int concurrencyLimit = 2, int? retryLimit = null)
        where TConsumer : class
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));

        // важна последовательность usage (вначале UseDelayedRedelivery, затем UseMessageRetry)
        configurator.UseDelayedRedelivery(redeliveryConfigurator =>
        {
            redeliveryConfigurator.Intervals(5.Minutes(), 15.Minutes(), 30.Minutes(), 1.Hours(), 3.Hours(), 6.Hours());
            redeliveryConfigurator.Handle(checkTransientException);
        });

        configurator.UseRetryConfiguration(checkTransientException, concurrencyLimit, retryLimit);
    }

    public static void UseRetryConfiguration<TConsumer>(this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException, int concurrencyLimit = 2, int? retryLimit = null)
        where TConsumer : class
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));
        if (concurrencyLimit >= 100) throw new ArgumentOutOfRangeException(nameof(concurrencyLimit));

        configurator.UseConcurrentMessageLimit(concurrencyLimit);

        configurator.UseMessageRetry(retryConfigurator =>
        {
            retryLimit ??= DefaultRetryLimit;

            retryConfigurator.Incremental(retryLimit.Value, 1.Seconds(), 1.Seconds());
            retryConfigurator.Handle(checkTransientException);
        });
    }

    /// <summary>
    /// Не использовать дефолтную настройку эндпоинта (concurrencyLimit = 1 и prefetchCount = 1) совместно с настройкой консьюмера с Redelivery,
    /// так как не будет гарантирована последовательная обработка сообщений (сообщение удалится из очедеди и консьюмер получит следующее)
    /// </summary>
    public static void UseLimitPrefetchConfiguration(this IEndpointConfigurator configurator, int concurrencyLimit = 1, int prefetchCount = 1)
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));
        if (concurrencyLimit >= 100) throw new ArgumentOutOfRangeException(nameof(concurrencyLimit));

        configurator.UseConcurrencyLimit(concurrencyLimit);
        configurator.ConfigureTransport(transportConfigurator => transportConfigurator.PrefetchCount = prefetchCount);
    }
}
