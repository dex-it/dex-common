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

    public virtual Task JobFail(
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

        if (envelope.Retries >= Options.Retries)
        {
            // Попытки исчерпаны. Хороним явным статусом, а не тем, что сообщение просто перестаёт
            // попадать в выборку: молчаливое исчезновение неотличимо от успеха при разборе инцидента.
            envelope.Status = InboxMessageStatus.DeadLettered;
            envelope.ScheduledStartIndexing = null;
            MetricCollector.IncDeadLetteredCount();
        }
        else
        {
            envelope.Status = InboxMessageStatus.Failed;

            var calculatedStartDate = _retryStrategy.CalculateNextStartDate(
                new InboxRetryStrategyOptions(envelope.StartAtUtc, envelope.Retries));
            envelope.StartAtUtc = calculatedStartDate;
            envelope.ScheduledStartIndexing = calculatedStartDate;
        }

        MetricCollector.IncProcessJobFailedCount();

        return CompleteJobAsync(inboxJob, cancellationToken);
    }

    public virtual Task JobSucceed(IInboxLockedJob inboxJob, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inboxJob);

        var envelope = inboxJob.Envelope;
        envelope.Status = InboxMessageStatus.Succeeded;
        envelope.Updated = DateTime.UtcNow;
        envelope.ErrorMessage = null;
        envelope.Error = null;

        // Выводим из выборки и из частичного индекса: строка остаётся только как ключ дедупликации.
        envelope.ScheduledStartIndexing = null;

        MetricCollector.IncProcessJobSuccessCount();

        return CompleteJobAsync(inboxJob, cancellationToken);
    }

    /// <summary>
    /// Зафиксировать исход обработки в хранилище, если аренда всё ещё принадлежит этой задаче.
    /// </summary>
    protected abstract Task CompleteJobAsync(IInboxLockedJob lockedJob, CancellationToken cancellationToken);
}
