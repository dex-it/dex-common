using System;
using System.Diagnostics.Metrics;
using System.Threading;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Inbox;

internal sealed class DefaultInboxMetricCollector : IInboxMetricCollector, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _processCount;
    private readonly Counter<long> _emptyProcessCount;
    private readonly Counter<long> _processJobCount;
    private readonly Counter<long> _processJobSuccessCount;
    private readonly Counter<long> _processJobFailedCount;
    private readonly Counter<long> _deadLetteredCount;
    private readonly Counter<long> _duplicateCount;
    private readonly Counter<long> _expiredBeforeStartCount;
    private readonly Counter<long> _leaseLostCount;
    private readonly Histogram<double> _processDuration;
    private long _lastTime = DateTime.UtcNow.Ticks;

    /// <summary>
    /// Завести счётчики инбокса.
    /// </summary>
    /// <remarks>
    /// Глубина очереди и число похороненных сообщений это UpDownCounter, а не Counter: обе величины
    /// растут на приёме и убывают на обработке или чистке, тогда как асинхронный Counter по спецификации
    /// обязан быть монотонно возрастающим, и экспортёр читает падение монотонного счётчика как его сброс,
    /// дорисовывая фантомный прирост. Gauge тоже неверен: глубина аддитивна по инстансам, а Gauge
    /// складывать не предполагается.
    /// </remarks>
    public DefaultInboxMetricCollector(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        _meter = new Meter("Inbox");
        _processCount = _meter.CreateCounter<long>("ProcessCount", description: "Count of Process call");
        _emptyProcessCount = _meter.CreateCounter<long>("EmptyProcessCount", description: "Count of Process call, without job processing");
        _processJobCount = _meter.CreateCounter<long>("ProcessJobCount", description: "Process job count");
        _processJobSuccessCount = _meter.CreateCounter<long>("ProcessJobSuccessCount", description: "Process job success count");
        _processJobFailedCount = _meter.CreateCounter<long>("ProcessJobFailedCount", description: "Process job failed count");
        _deadLetteredCount = _meter.CreateCounter<long>("DeadLetteredCount", description: "Count of messages moved to DeadLettered");
        _duplicateCount = _meter.CreateCounter<long>("DuplicateCount", description: "Count of rejected duplicate messages");
        _expiredBeforeStartCount = _meter.CreateCounter<long>(
            "ExpiredBeforeStartCount",
            description: "Count of messages whose lease expired while the claimed batch was draining, before the handler started");
        _leaseLostCount = _meter.CreateCounter<long>(
            "LeaseLostCount",
            description: "Count of messages whose lease was lost during processing, so the outcome could not be committed");

        _meter.CreateObservableUpDownCounter(
            "FreeJobCount",
            () => GetCount(p => p.GetFreeMessagesCount()),
            description: "Unprocessed jobs count this service can handle (inbox depth)");

        _meter.CreateObservableUpDownCounter(
            "DeadLetteredJobCount",
            () => GetCount(p => p.GetDeadLetteredMessagesCount()),
            description: "Dead lettered jobs waiting for manual review (cleanup never removes them)");

        _processDuration = _meter.CreateHistogram<double>("ProcessDuration", description: "Duration of job process, regardless of outcome");

        return;

        long GetCount(Func<IInboxDataProvider, int> read)
        {
            using var scope = scopeFactory.CreateScope();
            return read(scope.ServiceProvider.GetRequiredService<IInboxDataProvider>());
        }
    }

    public void IncProcessCount()
    {
        _processCount.Add(1);
        Interlocked.Exchange(ref _lastTime, DateTime.UtcNow.Ticks);
    }

    public void IncEmptyProcessCount()
    {
        _emptyProcessCount.Add(1);
        Interlocked.Exchange(ref _lastTime, DateTime.UtcNow.Ticks);
    }

    public void IncProcessJobCount()
    {
        _processJobCount.Add(1);
        Interlocked.Exchange(ref _lastTime, DateTime.UtcNow.Ticks);
    }

    public void IncProcessJobSuccessCount() => _processJobSuccessCount.Add(1);

    public void IncProcessJobFailedCount() => _processJobFailedCount.Add(1);

    public void IncDeadLetteredCount() => _deadLetteredCount.Add(1);

    public void IncDuplicateCount() => _duplicateCount.Add(1);

    public void IncExpiredBeforeStartCount() => _expiredBeforeStartCount.Add(1);

    public void IncLeaseLostCount() => _leaseLostCount.Add(1);

    public void AddProcessJobDuration(TimeSpan duration) => _processDuration.Record(duration.TotalMilliseconds);

    public DateTime GetLastStamp() => new(Interlocked.Read(ref _lastTime));

    public void Dispose()
    {
        _meter.Dispose();
    }
}