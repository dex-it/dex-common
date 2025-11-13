using System;

namespace Dex.Cap.Outbox.Interfaces;

internal interface IOutboxMetricCollector : IOutboxStatistic
{
    void IncProcessCount();
    void IncEmptyProcessCount();
    void IncProcessJobCount();
    void IncProcessJobSuccessCount();
    void AddProcessJobSuccessDuration(TimeSpan duration);
}

public interface IOutboxStatistic
{
    DateTime GetLastStamp();
}