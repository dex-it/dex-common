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

    /// <summary>
    /// Аренда потеряна к моменту фиксации исхода: обработка шла дольше аренды, и строку забрал другой обработчик.
    /// </summary>
    /// <remarks>
    /// Диагностирует ту же причину, что и <see cref="IncExpiredBeforeStartCount"/> (мал LockTimeout), но вторую её
    /// половину: там аренда умерла в очереди на обработку, здесь во время самой обработки. Без этого счётчика
    /// перехват аренды не виден в метриках вовсе, а ProcessJobCount переставал бы сходиться с суммой успехов и
    /// отказов без объяснимой причины.
    /// </remarks>
    void IncLeaseLostCount();

    void AddProcessJobDuration(TimeSpan duration);
}

/// <summary>
/// Признаки жизни обработчика инбокса. Используется health check'ом.
/// </summary>
public interface IInboxStatistic
{
    /// <summary>
    /// Момент начала последнего цикла обработки.
    /// </summary>
    /// <remarks>
    /// Именно начала, а не завершения: штамп это признак жизни обработчика, а не признак успеха. Цикл, который
    /// начался и завис в обработчике, обязан ронять health check по <c>Period * 2</c>, а не считаться живым до
    /// своего завершения.
    /// </remarks>
    DateTime GetLastStamp();
}
