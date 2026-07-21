using System;
using System.Linq;
using Dex.Cap.Outbox.AspNetScheduler;
using Dex.Cap.Outbox.AspNetScheduler.BackgroundServices;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Ef.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    // Маркер для однократной регистрации OutboxHealthCheck (guard от дублирования при повторном вызове AddOutboxScheduler).
    private sealed class OutboxHealthCheckRegistered;

    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, OutboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Регистрируем инфраструктуру опций, чтобы IOptions<OutboxOptions> резолвился для проверки размера
        // Content в OutboxService, а не оставался неявным требованием к вызывающему. Сама библиотека
        // регистрирует ЕДИНСТВЕННОЕ правило, про размер тела: оно заведено вместе с опцией, поэтому
        // конфигураций, которые оно отвергнет, в проде ещё нет. Остальные правила OutboxOptionsValidator НЕ
        // включаем: валидатор исторически не был подключён, и его включение отвергало бы значения, которые
        // раньше молча толерировались (например GetFreeMessagesTimeout ниже секунды), ломая существующих
        // потребителей на старте. Подключение валидатора целиком вынесено в issue #239.
        // ValidateOnStart при этом взводится на ТИП опций, а не на конкретный валидатор: на старте хоста
        // исполнятся ВСЕ зарегистрированные IValidateOptions<OutboxOptions>, включая те, что зарегистрировал
        // сам потребитель. Обойти это нельзя, материализация значения опций всегда прогоняет все валидаторы.
        services.AddOptions<OutboxOptions>()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OutboxOptions>, OutboxMaxContentLengthValidator>());

        services
            .AddSingleton<IOutboxMetricCollector, DefaultOutboxMetricCollector>()
            .AddSingleton<IOutboxTypeDiscriminatorProvider, OutboxTypeDiscriminatorProvider>()
            .AddSingleton<IOutboxStatistic>(provider => provider.GetRequiredService<IOutboxMetricCollector>())
            .AddScoped<IOutboxService, OutboxService>()
            .AddScoped<IOutboxEnvelopFactory, OutboxEnvelopFactory>()
            .AddScoped<IOutboxHandler, MainLoopOutboxHandler<TDbContext>>()
            .AddScoped<IOutboxJobHandler, OutboxJobHandlerEf<TDbContext>>()
            .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
            .AddScoped<IOutboxDataProvider, OutboxDataProviderEf<TDbContext>>()
            .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>()
            .AddScoped<IOutboxRetryStrategy>(provider =>
            {
                var retryStrategyConfigurator = new OutboxRetryStrategyConfigurator();
                retryStrategyImplementation?.Invoke(provider, retryStrategyConfigurator);

                return retryStrategyConfigurator.RetryStrategy;
            });

        return services;
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection AddDefaultOutboxScheduler<TDbContext>(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TDbContext : DbContext
    {
        return AddOutboxScheduler<OutboxCleanupDataProviderEf<TDbContext>>(services, periodSeconds, cleanupDays);
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance. Configure scheduler options explicitly
    /// (period, cleanup, and init-delay ranges).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddDefaultOutboxScheduler<TDbContext>(this IServiceCollection services, Action<OutboxHandlerOptions> configure)
        where TDbContext : DbContext
    {
        return AddOutboxScheduler<OutboxCleanupDataProviderEf<TDbContext>>(services, configure);
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection AddOutboxScheduler<TCleanUpDataProvider>(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TCleanUpDataProvider : class, IOutboxCleanupDataProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        if (periodSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds), periodSeconds, "Should be a positive number");

        if (cleanupDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(cleanupDays), cleanupDays, "Should be a positive number");

        return AddOutboxScheduler<TCleanUpDataProvider>(services, options =>
        {
            options.Period = TimeSpan.FromSeconds(periodSeconds);
            options.CleanupOlderThan = TimeSpan.FromDays(cleanupDays);
            options.CleanupInterval = TimeSpan.FromHours(1);
        });
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance. Configure scheduler options explicitly
    /// (period, cleanup, and init-delay ranges).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOutboxScheduler<TCleanUpDataProvider>(this IServiceCollection services, Action<OutboxHandlerOptions> configure)
        where TCleanUpDataProvider : class, IOutboxCleanupDataProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.All(d => d.ServiceType != typeof(OutboxHealthCheckRegistered)))
        {
            services.AddSingleton<OutboxHealthCheckRegistered>();

            services
                .AddHealthChecks()
                .AddCheck<OutboxHealthCheck>("outbox-scheduler", tags: ["outbox-scheduler"]);
        }

        services.AddOptions<OutboxHandlerOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OutboxHandlerOptions>, OutboxHandlerOptionsValidator>());

        services
            .AddScoped<IOutboxCleanupDataProvider, TCleanUpDataProvider>()
            .AddScoped<IOutboxCleanerHandler, OutboxCleanerHandler>()
            .AddHostedService<OutboxHandlerBackgroundService>()
            .AddHostedService<OutboxCleanerBackgroundService>();

        return services;
    }
}