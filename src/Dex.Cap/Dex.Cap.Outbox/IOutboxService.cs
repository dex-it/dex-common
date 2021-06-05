using System;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService
    {
        Task Enqueue<T>(T message, Guid correlationId) where T : IOutboxMessage;
    }
}