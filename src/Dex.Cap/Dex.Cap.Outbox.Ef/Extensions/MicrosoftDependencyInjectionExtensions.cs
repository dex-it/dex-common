using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider,
        Action<IServiceProvider, OutboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        serviceProvider
            .AddSingleton<IOutboxMetricCollector, DefaultOutboxMetricCollector>()
            .AddSingleton<IOutboxTypeDiscriminatorProvider, OutboxTypeDiscriminatorProvider>()
            .AddSingleton<IOutboxStatistic>(provider => provider.GetRequiredService<IOutboxMetricCollector>())
            .AddScoped<IOutboxService, OutboxService>()
            .AddScoped<IOutboxHandler, MainLoopOutboxHandler<TDbContext>>()
            .AddScoped<IOutboxJobHandler, OutboxJobHandlerEf<TDbContext>>()
            .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
            .AddScoped<IOutboxDataProvider, OutboxDataProviderEf<TDbContext>>()
            .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();

        serviceProvider.AddScoped<IOutboxRetryStrategy>(provider =>
        {
            var retryStrategyConfigurator = new OutboxRetryStrategyConfigurator();
            retryStrategyImplementation?.Invoke(provider, retryStrategyConfigurator);

            return retryStrategyConfigurator.RetryStrategy;
        });

        return serviceProvider;
    }

    public static IServiceCollection AddDefaultCleanUpDataProvider<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        
        return services.AddCleanUpDataProvider<OutboxCleanupDataProviderEf<TDbContext>>();
    }

    public static IServiceCollection AddCleanUpDataProvider<TCleanUpDataProvider>(this IServiceCollection services) where TCleanUpDataProvider : class, IOutboxCleanupDataProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        
        return services.AddScoped<IOutboxCleanupDataProvider, TCleanUpDataProvider>();
    }
}