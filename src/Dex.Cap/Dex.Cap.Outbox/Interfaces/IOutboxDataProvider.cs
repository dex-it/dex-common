using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Interfaces
{
    internal interface IOutboxDataProvider<TOptions, TDbContext> : IOutboxDataProvider
        where TOptions : ITransactionOptions
    {
        Task ExecuteActionInTransaction<TState>(
            Guid correlationId,
            IOutboxService<TOptions, TDbContext> outboxService,
            TState state,
            Func<IOutboxContext<TDbContext, TState>, CancellationToken, Task> action,
            TOptions? options = default,
            CancellationToken cancellationToken = default);
    }

    internal interface IOutboxDataProvider
    {
        Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken = default);
        Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken = default);
        Task<IOutboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken = default);

        Task JobFail(IOutboxLockedJob outboxJob, string? errorMessage = null, Exception? exception = null,
            CancellationToken cancellationToken = default);

        Task JobSucceed(IOutboxLockedJob outboxJob, CancellationToken cancellationToken = default);
        Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken = default);
        int GetFreeMessagesCount();
    }
}