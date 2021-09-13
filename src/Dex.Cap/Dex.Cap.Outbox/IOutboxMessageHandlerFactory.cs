using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageHandlerFactory
    {
        IOutboxMessageHandler GetMessageHandler(IOutboxMessage outboxMessage);
    }
}