using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageProcessor
    {
        Task ProcessMessage(Models.Outbox outbox);
    }
}