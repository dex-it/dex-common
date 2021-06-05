namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageHandlerFactory
    {
        IOutboxMessageHandler GetMessageHandler(IOutboxMessage outboxMessage);
    }
}