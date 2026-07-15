using System;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxMetricCollector : IInboxStatistic
{
    void IncProcessCount();
    void IncEmptyProcessCount();
    void IncProcessJobCount();
    void IncProcessJobSuccessCount();
    void IncProcessJobFailedCount();
    void IncDeadLetteredCount();
    void IncDuplicateCount();
    void AddProcessJobDuration(TimeSpan duration);
}

public interface IInboxStatistic
{
    DateTime GetLastStamp();
}
