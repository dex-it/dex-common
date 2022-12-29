using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Interfaces
{
    internal interface IOutboxDataProvider<TDbContext> : IOutboxDataProvider
    {
        Task ExecuteActionInTransaction<TState>(Guid corellationId, IOutboxService<TDbContext> outboxService, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken);
    }
    
    internal interface IOutboxDataProvider
    {
        Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);
        Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);
        Task JobFail(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null);
        Task JobSucceed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken);

        Task<OutboxEnvelope[]> GetFreeMessages(int limit, CancellationToken cancellationToken);
        int GetFreeMessagesCount();
    }
}