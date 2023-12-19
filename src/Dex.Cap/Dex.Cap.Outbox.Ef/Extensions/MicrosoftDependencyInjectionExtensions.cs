using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef.Extensions
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider,
            Action<IServiceProvider, OutboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
            where TDbContext : DbContext
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            serviceProvider
                .AddSingleton<IOutboxMetricCollector, DefaultOutboxMetricCollector>()
                .AddSingleton<IOutboxStatistic>(provider => provider.GetRequiredService<IOutboxMetricCollector>())
                .AddScoped<IOutboxService<TDbContext>, OutboxService<TDbContext>>()
                .AddScoped<IOutboxService>(provider => provider.GetRequiredService<IOutboxService<TDbContext>>())
                .AddScoped<OutboxTypeDiscriminator<string>>()
                .AddScoped<IOutboxHandler, OutboxHandler<TDbContext>>()
                .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
                .AddScoped<IOutboxDataProvider<TDbContext>, OutboxDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxDataProvider>(provider => provider.GetRequiredService<IOutboxDataProvider<TDbContext>>())
                .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();

            serviceProvider.AddScoped<IOutboxRetryStrategy>(provider =>
            {
                var retryStrategyConfigurator = new OutboxRetryStrategyConfigurator();
                retryStrategyImplementation?.Invoke(provider, retryStrategyConfigurator);

                return retryStrategyConfigurator.RetryStrategy;
            });

            return serviceProvider;
        }
    }
}