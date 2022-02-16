using App.Metrics;
using App.Metrics.AspNetCore;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Dex.Cap.Outbox.Ef
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider)
            where TDbContext : DbContext
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            serviceProvider.AddHealthChecks()
                .AddCheck<OutboxHealthCheck>("outbox-scheduler");
            
            return serviceProvider
                .AddScoped<IOutboxService, OutboxService>()
                .AddScoped<IOutboxHandler, OutboxHandler<TDbContext>>()
                .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
                .AddScoped<IOutboxDataProvider, OutboxDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();
        }

        public static IHostBuilder AddOutboxMetrics(this IHostBuilder hostBuilder, IMetricsRoot metricsRoot, Action<MetricsWebHostOptions> optionsDelegate)
        {
            metricsRoot.Measure.Gauge.SetValue(MetricsRegistry.UnprocessedMessages, 0);

            return hostBuilder
                .ConfigureMetrics(metricsRoot)
                .UseMetrics(optionsDelegate);
        }
    }
}