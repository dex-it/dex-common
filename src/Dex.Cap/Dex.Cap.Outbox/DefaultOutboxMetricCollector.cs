using System;
using System.Diagnostics.Metrics;
using System.Threading;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox
{
    internal sealed class DefaultOutboxMetricCollector : IOutboxMetricCollector, IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _processCount;
        private readonly Counter<long> _emptyProcessCount;
        private readonly Counter<long> _processJobCount;
        private readonly Counter<long> _processJobSuccessCount;
        private readonly Histogram<double> _processDuration;
        private long _lastTime = DateTime.UtcNow.Ticks;

        public DefaultOutboxMetricCollector(IServiceScopeFactory scopeFactory)
        {
            _meter = new Meter("Outbox");
            _processCount = _meter.CreateCounter<long>("ProcessCount", description: "Count of Process call");
            _emptyProcessCount = _meter.CreateCounter<long>("EmptyProcessCount", description: "Count of Process call, without job processing");
            _processJobCount = _meter.CreateCounter<long>("ProcessJobCount", description: "Process job count");
            _processJobSuccessCount = _meter.CreateCounter<long>("ProcessJobSuccessCount", description: "Process job success count");
            _meter.CreateObservableCounter<long>("FreeJobCount", () => GetFreeMessagesCount(), description: "Unpocessed jobs count");
            _processDuration = _meter.CreateHistogram<double>("ProcessDuration", description: "Duration of success job process");

            int GetFreeMessagesCount()
            {
                using var scope = scopeFactory.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IOutboxDataProvider>()
                    .GetFreeMessagesCount();
            }
        }

        public void IncProcessCount()
        {
            _processCount.Add(1);
            Interlocked.Exchange(ref _lastTime, DateTime.UtcNow.Ticks);
        }

        public void IncEmptyProcessCount() => _emptyProcessCount.Add(1);

        public void IncProcessJobCount()
        {
            _processJobCount.Add(1);
            Interlocked.Exchange(ref _lastTime, DateTime.UtcNow.Ticks);
        }

        public void IncProcessJobSuccessCount() => _processJobSuccessCount.Add(1);
        public void AddProcessJobSuccessDuration(TimeSpan duration) => _processDuration.Record(duration.TotalMilliseconds);

        public DateTime GetLastStamp() => new(Interlocked.Read(ref _lastTime));

        public void Dispose()
        {
            _meter.Dispose();
        }
    }
}