using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandler
    {
        Task ProcessMessage(IOutboxMessage outboxMessage, CancellationToken cancellationToken);
    }
}