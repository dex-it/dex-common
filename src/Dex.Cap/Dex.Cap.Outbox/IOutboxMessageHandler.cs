using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageHandler<in T> : IOutboxMessageHandler where T : IOutboxMessage
    {
        Task ProcessMessage(T message);
    }

    public interface IOutboxMessageHandler
    {
        Task ProcessMessage(IOutboxMessage outbox);
    }
    
    public interface IOutboxMessage
    {
    }

}