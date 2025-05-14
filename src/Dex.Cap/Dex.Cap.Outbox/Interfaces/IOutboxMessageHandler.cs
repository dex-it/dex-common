using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler<in TMessage>
        where TMessage : class
    {
        Task Process(TMessage message, CancellationToken cancellationToken);
    }
}