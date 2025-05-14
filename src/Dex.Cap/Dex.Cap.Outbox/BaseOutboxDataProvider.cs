using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox
{
    internal abstract class BaseOutboxDataProvider<TDbContext>(IOutboxRetryStrategy retryStrategy) : IOutboxDataProvider<TDbContext>
    {
        private readonly IOutboxRetryStrategy _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));

        public abstract Task ExecuteActionInTransaction<TState>(Guid correlationId, IOutboxService<TDbContext> outboxService,
            TState state, Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken);

        public abstract Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        public abstract Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);

        // Job management

        public abstract Task<IOutboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken);

        public virtual Task JobFail(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null,
            Exception? exception = null)
        {
            ArgumentNullException.ThrowIfNull(outboxJob);

            outboxJob.Envelope.Status = OutboxMessageStatus.Failed;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = errorMessage;
            outboxJob.Envelope.Error = exception?.ToString();

            var calculatedStartDate = _retryStrategy
                .CalculateNextStartDate(new OutboxRetryStrategyOptions(outboxJob.Envelope.StartAtUtc, outboxJob.Envelope.Retries));
            outboxJob.Envelope.StartAtUtc = calculatedStartDate;
            outboxJob.Envelope.ScheduledStartIndexing = calculatedStartDate;

            return CompleteJobAsync(outboxJob, cancellationToken);
        }

        public virtual Task JobSucceed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(outboxJob);

            outboxJob.Envelope.Status = OutboxMessageStatus.Succeeded;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = null;
            outboxJob.Envelope.Error = null;
            outboxJob.Envelope.ScheduledStartIndexing = null;

            return CompleteJobAsync(outboxJob, cancellationToken);
        }

        protected abstract Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken);

        public abstract Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken token);

        public abstract int GetFreeMessagesCount();
    }
}