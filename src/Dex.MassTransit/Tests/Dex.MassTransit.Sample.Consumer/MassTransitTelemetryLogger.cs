using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Sample.Consumer
{
    internal class MassTransitTelemetryLogger : IObserver<DiagnosticListener>
    {
        private readonly MetricTracer _metricTracer;

        public MassTransitTelemetryLogger(ILogger<MassTransitTelemetryLogger> logger)
        {
            _metricTracer = new MetricTracer(logger);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "MassTransit")
            {
                value.Subscribe(_metricTracer);
            }
        }

        private class MetricTracer : IObserver<KeyValuePair<string, object>>
        {
            private readonly ILogger<MassTransitTelemetryLogger> _logger;

            public MetricTracer(ILogger<MassTransitTelemetryLogger> logger)
            {
                _logger = logger;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object> pair)
            {
                if (pair.Key.Length > 0)
                {
                    if (pair.Key == "MassTransit.Transport.Receive.Start")
                    {
                        _logger.Log(LogLevel.Information, $"{pair.Key}: {pair.Value}");
                    }

                    if (pair.Key == "MassTransit.Transport.Send.Start")
                    {
                        _logger.Log(LogLevel.Information, $"G: {pair.Key}: {pair.Value}");
                    }
                }
            }
        }
    }
}