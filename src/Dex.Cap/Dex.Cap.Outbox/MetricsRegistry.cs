using App.Metrics;
using App.Metrics.Gauge;

namespace Dex.Cap.Outbox
{
    public static class MetricsRegistry
    {
        public static readonly GaugeOptions UnprocessedMessages = new()
        {
            Name = "Unprocessed messages",
            MeasurementUnit = Unit.Items
        };
    }
}
