using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler
    {
        bool IsTransactional => false;
        Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken);
    }
}