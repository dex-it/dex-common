using System.Linq;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using Dex.Cap.Outbox.Ef;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dex.Cap.AspNet.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var metricsRoot = AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .OutputMetrics.AsPrometheusProtobuf()
                .Filter.With(metrics => metrics.WhereContext("Application"))
                .Build();

            CreateHostBuilder(args)
                .AddOutboxMetrics(metricsRoot, options =>
                {
                    options.EndpointOptions = endpointsOptions =>
                    {
                        endpointsOptions.MetricsTextEndpointOutputFormatter = metricsRoot.OutputMetricsFormatters
                            .OfType<MetricsPrometheusTextOutputFormatter>().First();
                        endpointsOptions.MetricsEndpointOutputFormatter = metricsRoot.OutputMetricsFormatters
                            .OfType<MetricsPrometheusProtobufOutputFormatter>().First();
                    };
                })
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
