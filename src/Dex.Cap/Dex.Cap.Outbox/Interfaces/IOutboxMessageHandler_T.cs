using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler<in TMessage> : IOutboxMessageHandler where TMessage : IOutboxMessage
    {
        Task ProcessMessage(TMessage message, CancellationToken cancellationToken);
    }
}