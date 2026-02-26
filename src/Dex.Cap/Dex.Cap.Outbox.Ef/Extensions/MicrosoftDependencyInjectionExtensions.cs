using System;
using Dex.Cap.Outbox.AspNetScheduler;
using Dex.Cap.Outbox.AspNetScheduler.BackgroundServices;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, OutboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

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

        services
            .AddHealthChecks()
            .AddCheck<OutboxHealthCheck>("outbox-scheduler", tags: ["outbox-scheduler"]);

        services
            .AddSingleton(new OutboxHandlerOptions
            {
                Period = TimeSpan.FromSeconds(periodSeconds),
                CleanupOlderThan = TimeSpan.FromDays(cleanupDays),
                CleanupInterval = TimeSpan.FromHours(1)
            })
            .AddScoped<IOutboxCleanupDataProvider, TCleanUpDataProvider>()
            .AddScoped<IOutboxCleanerHandler, OutboxCleanerHandler>()
            .AddHostedService<OutboxHandlerBackgroundService>()
            .AddHostedService<OutboxCleanerBackgroundService>();

        return services;
    }
}