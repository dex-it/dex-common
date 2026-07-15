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

    /// <summary>
    /// Сообщение даже не начали обрабатывать: аренда истекла, пока дренировалась партия.
    /// </summary>
    /// <remarks>Устойчиво ненулевое значение означает, что LockTimeout мал для MessagesToProcess.</remarks>
    void IncExpiredBeforeStartCount();

    void AddProcessJobDuration(TimeSpan duration);
}

public interface IInboxStatistic
{
    DateTime GetLastStamp();
}
