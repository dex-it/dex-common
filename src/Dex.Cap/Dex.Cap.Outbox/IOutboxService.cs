using System;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService<TDbContext>
    {
        Task Publish<T>(T message, Guid correlationId);
        Task Send<T>(T message, Guid correlationId);
    }
}