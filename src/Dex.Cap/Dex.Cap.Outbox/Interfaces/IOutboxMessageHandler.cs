using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler<in TMessage> : IOutboxMessageHandler where TMessage : IOutboxMessage
    {
        Task ProcessMessage(TMessage message, CancellationToken cancellationToken);
    }

    public interface IOutboxMessageHandler
    {
        Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken);
    }
}