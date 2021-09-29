using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Interfaces
{
    internal interface IOutboxDataProvider
    {
        Task ExecuteInTransaction(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken);

        Task ExecuteUsefulAndSaveOutboxActionIntoTransaction<TContext, TOutboxMessage>(Guid correlationId, Func<CancellationToken, Task<TContext>> usefulAction,
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData, CancellationToken cancellationToken)
            where TContext : class where TOutboxMessage : IOutboxMessage;

        Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        Task Fail(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null);

        Task Succeed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken);

        /// <exception cref="OperationCanceledException"/>
        IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);

        Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);
    }
}