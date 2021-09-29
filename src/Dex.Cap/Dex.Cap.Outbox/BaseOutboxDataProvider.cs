using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal abstract class BaseOutboxDataProvider : IOutboxDataProvider
    {
      public abstract Task ExecuteUsefulAndSaveOutboxActionIntoTransaction<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken)
            where TContext : class
            where TOutboxMessage : IOutboxMessage;

        public abstract Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        public abstract Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);

        // Job management

        public abstract IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);

        public virtual async Task JobFail(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null,
            Exception? exception = null)
        {
            if (outboxJob == null)
            {
                throw new ArgumentNullException(nameof(outboxJob));
            }

            outboxJob.Envelope.Status = OutboxMessageStatus.Failed;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = errorMessage;
            outboxJob.Envelope.Error = exception?.ToString();

            await CompleteJobAsync(outboxJob, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task JobSucceed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken)
        {
            if (outboxJob == null)
            {
                throw new ArgumentNullException(nameof(outboxJob));
            }

            outboxJob.Envelope.Status = OutboxMessageStatus.Succeeded;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = null;
            outboxJob.Envelope.Error = null;

            await CompleteJobAsync(outboxJob, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken);

        public virtual bool IsDbTransientError(Exception ex)
        {
            return false;
        }
    }
}