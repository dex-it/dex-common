using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxHandler
    {
        Task Process(CancellationToken cancellationToken);
    }
}