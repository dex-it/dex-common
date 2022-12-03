using System;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider) where TDbContext : DbContext
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            return serviceProvider
                .AddSingleton<IOutboxMetricCollector, DefaultOutboxMetricCollector>()
                .AddSingleton<IOutboxStatistic>(provider => provider.GetRequiredService<IOutboxMetricCollector>())
                .AddScoped<IOutboxService<TDbContext>, OutboxService<TDbContext>>()
                .AddScoped<IOutboxHandler, OutboxHandler<TDbContext>>()
                .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
                .AddScoped<IOutboxDataProvider<TDbContext>, OutboxDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxDataProvider>(provider => provider.GetRequiredService<IOutboxDataProvider<TDbContext>>())
                .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();
        }
    }
}