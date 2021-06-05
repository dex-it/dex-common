using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService
    {
        Task Enqueue<T>(T message, Guid correlationId, CancellationToken cancellationToken) where T : IOutboxMessage;
        Task Enqueue<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage;
    }
}