using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler
    {
        Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken);
    }
}