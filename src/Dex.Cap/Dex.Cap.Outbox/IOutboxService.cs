using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService
    {
        Task<Guid> Enqueue<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage;
        Task<Guid> Enqueue<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage;
        Task<bool> IsOperationExists(Guid correlationId, CancellationToken cancellationToken);
    }
}