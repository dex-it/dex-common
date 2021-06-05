using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageHandler<in T> : IOutboxMessageHandler where T : IOutboxMessage
    {
        Task ProcessMessage(T message, CancellationToken cancellationToken);
    }

    public interface IOutboxMessageHandler
    {
        Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken);
    }

    public interface IOutboxMessage
    {
    }
}