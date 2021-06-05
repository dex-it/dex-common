using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Test
{
    internal class MetricTraceExporterHostedService : IHostedService
    {
        public MetricTraceExporterHostedService(MassTransitTelemetryLogger massTransitTelemetryLogger)
        {
            DiagnosticListener.AllListeners.Subscribe(massTransitTelemetryLogger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}