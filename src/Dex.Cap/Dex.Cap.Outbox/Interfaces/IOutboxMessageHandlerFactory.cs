namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessageHandlerFactory
    {
        IOutboxMessageHandler GetMessageHandler(IOutboxMessage outboxMessage);
    }
}