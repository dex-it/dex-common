using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Filters;
using App.Metrics.Formatters.Prometheus;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

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

        public static IHostBuilder AddOutboxMetrics(this IHostBuilder hostBuilder,
            Action<IFilterMetrics> optionsDelegate)
        {
            var metrics = AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .OutputMetrics.AsPrometheusProtobuf()
                .Filter.With(optionsDelegate)
                .Build();

            return AddOutboxMetricsInternal(hostBuilder, metrics);
        }

        public static IHostBuilder AddOutboxMetrics(this IHostBuilder hostBuilder)
        {
            var metrics = AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .OutputMetrics.AsPrometheusProtobuf()
                .Build();

            return AddOutboxMetricsInternal(hostBuilder, metrics);
        }

        private static IHostBuilder AddOutboxMetricsInternal(IHostBuilder hostBuilder, IMetricsRoot metricsRoot)
        {
            metricsRoot.Measure.Gauge.SetValue(MetricsRegistry.UnprocessedMessages, 0);

            return hostBuilder
                .ConfigureMetrics(metricsRoot)
                .UseMetrics(
                    options =>
                    {
                        options.EndpointOptions = endpointsOptions =>
                        {
                            endpointsOptions.MetricsTextEndpointOutputFormatter = metricsRoot.OutputMetricsFormatters
                                .OfType<MetricsPrometheusTextOutputFormatter>().First();
                            endpointsOptions.MetricsEndpointOutputFormatter = metricsRoot.OutputMetricsFormatters
                                .OfType<MetricsPrometheusProtobufOutputFormatter>().First();
                        };
                    });
        }
    }
}