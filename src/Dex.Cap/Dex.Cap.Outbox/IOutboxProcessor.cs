using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxProcessor
    {
        Task Process();
    }
}