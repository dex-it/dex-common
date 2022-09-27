using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Interfaces
{
    internal interface IOutboxDataProvider<TDbContext>
    {
        Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);

        Task ExecuteUsefulAndSaveOutboxActionIntoTransaction<TState, TDataContext, TOutboxMessage>(Guid correlationId,
            IOutboxService<TDbContext> outboxService, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task<TDataContext>> usefulAction,
            Func<CancellationToken, TDataContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken)
            where TOutboxMessage : IOutboxMessage;

        Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        /// <exception cref="OperationCanceledException"/>
        IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);

        Task JobFail(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null);

        Task JobSucceed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken);

        Task<OutboxEnvelope[]> GetFreeMessages(int limit, CancellationToken cancellationToken);
    }
}