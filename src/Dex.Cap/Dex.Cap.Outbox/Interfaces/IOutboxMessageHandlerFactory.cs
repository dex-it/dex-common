namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxMessageHandlerFactory
{
    object GetMessageHandler(object outboxMessage);
}