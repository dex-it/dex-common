using System;
using Dex.Extensions;
using MassTransit;

namespace Dex.MassTransit.Rabbit;

public static class MassTransitConfigurationExtensions
{
    private const int DefaultRetryLimit = 3;

    private static readonly TimeSpan[] DefaultRedeliveryIntervals = [5.Minutes(), 15.Minutes(), 30.Minutes(), 1.Hours(), 3.Hours(), 6.Hours()];

    private static readonly RetryExponentialIntervals DefaultRetryIntervals = new(1.Seconds(), 5.Seconds(), 1.Seconds());

    /// <summary>
    /// Конфигурация с настроенными Redelivery и Retry
    /// <remarks>
    /// Используйте TransientExceptionsHandler для перехвата временных ошибок.
    /// Исключения MassTransit проверяются политикой как в <see cref="HandleTransient"/>.
    /// </remarks>
    /// </summary>
    public static IConsumerConfigurator UseRedeliveryRetryConfiguration<TConsumer>(
        this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException,
        int? retryLimit = null,
        RetryExponentialIntervals? retryIntervals = null,
        TimeSpan[]? redeliveryIntervals = null)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        // важна последовательность usage (вначале UseDelayedRedelivery, затем UseMessageRetry)
        configurator.UseDelayedRedelivery(redeliveryConfigurator =>
        {
            redeliveryConfigurator.Intervals(redeliveryIntervals ?? DefaultRedeliveryIntervals);
            redeliveryConfigurator.HandleTransient(checkTransientException);
        });

        configurator.UseRetryConfiguration(checkTransientException, retryLimit, retryIntervals);

        return configurator;
    }

    /// <summary>
    /// Конфигурация Retry
    /// <remarks>
    /// Используйте TransientExceptionsHandler для перехвата временных ошибок.
    /// Исключения MassTransit проверяются политикой как в <see cref="HandleTransient"/>.
    /// </remarks>
    /// </summary>
    public static IConsumerConfigurator UseRetryConfiguration<TConsumer>(
        this IConsumerConfigurator<TConsumer> configurator,
        Func<Exception, bool> checkTransientException,
        int? retryLimit = null,
        RetryExponentialIntervals? retryIntervals = null)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.UseMessageRetry(retryConfigurator =>
        {
            retryLimit ??= DefaultRetryLimit;
            var intervals = retryIntervals ?? DefaultRetryIntervals;

            retryConfigurator.Exponential(retryLimit.Value, intervals.MinInterval, intervals.MaxInterval, intervals.Delta);
            retryConfigurator.HandleTransient(checkTransientException);
        });

        return configurator;
    }

    /// <summary>
    /// Повторять исключения, которые <paramref name="checkTransientException"/> считает временными, — для любого
    /// UseMessageRetry или UseDelayedRedelivery: консьюмера, обработчика, саги, endpoint.
    /// </summary>
    /// <remarks>
    /// Отмену внутри консьюмера, обработчика, саги и по UseTimeout MassTransit до фильтров повтора подменяет на
    /// <see cref="ConsumerCanceledException"/> без вложенного исключения, а таймаут request client — не <see cref="TimeoutException"/>.
    /// Политика сначала проверяет исходное исключение, а при отказе — ещё раз: <see cref="ConsumerCanceledException"/> как
    /// <see cref="OperationCanceledException"/>, <see cref="RequestTimeoutException"/> как <see cref="TimeoutException"/>;
    /// исходное — во вложенном.
    /// </remarks>
    public static void HandleTransient(this IExceptionConfigurator configurator, Func<Exception, bool> checkTransientException)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(checkTransientException);

        configurator.Handle<Exception>(exception => checkTransientException(exception) || exception switch
        {
            ConsumerCanceledException => checkTransientException(new OperationCanceledException(exception.Message, exception)),
            RequestTimeoutException => checkTransientException(new TimeoutException(exception.Message, exception)),
            _ => false,
        });
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
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(concurrencyLimit, 100);

        configurator.UseConcurrencyLimit(concurrencyLimit);
        configurator.ConfigureTransport(transportConfigurator => transportConfigurator.PrefetchCount = prefetchCount);

        return configurator;
    }
}

public readonly record struct RetryExponentialIntervals(TimeSpan MinInterval, TimeSpan MaxInterval, TimeSpan Delta);