using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public interface IOutboxDataProvider
    {
        Task ExecuteInTransactionAsync(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken);

        Task<OutboxEnvelope> AddAsync(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        
        Task FailAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null);
        
        Task SucceedAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken);

        /// <exception cref="OperationCanceledException"/>
        IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);
        
        Task<bool> IsExistsAsync(Guid correlationId, CancellationToken cancellationToken);

        /// <returns>Число удалённых записей.</returns>
        /// <exception cref="OperationCanceledException"/>
        Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken);
    }
}