using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Jobs;
using Dex.Cap.Inbox.Models;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox;

/// <summary>
/// Storage-agnostic политика завершения задач инбокса.
/// </summary>
/// <remarks>
/// Здесь живёт решение «повторить или похоронить», а весь ввод-вывод сведён к <see cref="CompleteJobAsync"/>,
/// который реализует конкретный провайдер хранилища.
/// </remarks>
internal abstract class BaseInboxDataProvider : IInboxDataProvider
{
    private readonly IInboxRetryStrategy _retryStrategy;

    protected InboxOptions Options { get; }

    protected IInboxMetricCollector MetricCollector { get; }

    protected BaseInboxDataProvider(
        IInboxRetryStrategy retryStrategy,
        IOptions<InboxOptions> options,
        IInboxMetricCollector metricCollector)
    {
        ArgumentNullException.ThrowIfNull(options);

        _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
        MetricCollector = metricCollector ?? throw new ArgumentNullException(nameof(metricCollector));
        Options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public abstract Task<InboxEnqueueStatus> Add(InboxEnvelope inboxEnvelope, CancellationToken cancellationToken);

    public abstract Task<IInboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken);

    public abstract int GetFreeMessagesCount();

    public abstract int GetDeadLetteredMessagesCount();

    /// <inheritdoc />
    /// <remarks>
    /// Аренда не требуется: неудача фиксируется после отката транзакции обработчика, ронять нечего. Если
    /// аренда уже у другого обработчика, запись не состоится и он отработает сообщение сам.
    /// </remarks>
    public virtual async Task JobFail(
        IInboxLockedJob inboxJob,
        string? errorMessage = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inboxJob);

        var envelope = inboxJob.Envelope;
        envelope.Updated = DateTime.UtcNow;
        envelope.Retries++;
        envelope.ErrorMessage = errorMessage;
        envelope.Error = exception?.ToString();

        var deadLettered = envelope.Retries >= Options.Retries;

        if (deadLettered)
        {
            DeadLetter(envelope);
        }
        else
        {
            ScheduleRetry(envelope);
        }

        await CompleteJobAsync(inboxJob, requireLease: false, cancellationToken).ConfigureAwait(false);

        MetricCollector.IncProcessJobFailedCount();

        if (deadLettered)
        {
            MetricCollector.IncDeadLetteredCount();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Аренда требуется: вызов идёт внутри транзакции обработчика, и если аренда потеряна, единственный
    /// способ не применить эффект дважды это откатить транзакцию исключением.
    /// <para>
    /// Сброс ScheduledStartIndexing выводит сообщение из выборки и из частичного индекса: строка остаётся
    /// только как ключ дедупликации.
    /// </para>
    /// </remarks>
    public virtual async Task JobSucceed(IInboxLockedJob inboxJob, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inboxJob);

        var envelope = inboxJob.Envelope;
        envelope.Status = InboxMessageStatus.Succeeded;
        envelope.Updated = DateTime.UtcNow;
        envelope.ErrorMessage = null;
        envelope.Error = null;
        envelope.ScheduledStartIndexing = null;

        await CompleteJobAsync(inboxJob, requireLease: true, cancellationToken).ConfigureAwait(false);

        MetricCollector.IncProcessJobSuccessCount();
    }

    /// <summary>
    /// Попытки исчерпаны: сообщение выводится из обработки до ручного разбора.
    /// </summary>
    /// <remarks>
    /// Статус проставляется явно, а не подразумевается тем, что сообщение просто перестаёт попадать в
    /// выборку: молчаливое исчезновение неотличимо от успеха при разборе инцидента.
    /// </remarks>
    private static void DeadLetter(InboxEnvelope envelope)
    {
        envelope.Status = InboxMessageStatus.DeadLettered;
        envelope.ScheduledStartIndexing = null;
    }

    /// <summary>
    /// Запланировать следующую попытку.
    /// </summary>
    /// <remarks>
    /// Задержка отсчитывается от момента отказа, а не от прежнего StartAtUtc. Отсчёт от расписания даёт
    /// задержку только пока обработка успевает за расписанием: стоит ей отстать (бэклог, долгий
    /// обработчик), и StartAtUtc + delay оказывается в прошлом, то есть повтор идёт мгновенно и все
    /// попытки сгорают за миллисекунды. Backoff нужен ровно в этом случае, поэтому он не может от него
    /// зависеть.
    /// </remarks>
    private void ScheduleRetry(InboxEnvelope envelope)
    {
        envelope.Status = InboxMessageStatus.Failed;

        var nextStartDate = _retryStrategy.CalculateNextStartDate(
            new InboxRetryStrategyOptions(DateTime.UtcNow, envelope.Retries));

        envelope.StartAtUtc = nextStartDate;
        envelope.ScheduledStartIndexing = nextStartDate;
    }

    /// <summary>
    /// Зафиксировать исход обработки в хранилище, если аренда всё ещё принадлежит этой задаче.
    /// </summary>
    /// <param name="lockedJob">Задача с захваченной арендой.</param>
    /// <param name="requireLease">
    /// <see langword="true"/> - потеря аренды это ошибка, реализация обязана бросить
    /// <see cref="Exceptions.InboxLeaseLostException"/>. <see langword="false"/> - потеря аренды
    /// это штатный исход, запись просто не состоится.
    /// </param>
    /// <param name="cancellationToken">Токен отмены.</param>
    protected abstract Task CompleteJobAsync(IInboxLockedJob lockedJob, bool requireLease, CancellationToken cancellationToken);
}
