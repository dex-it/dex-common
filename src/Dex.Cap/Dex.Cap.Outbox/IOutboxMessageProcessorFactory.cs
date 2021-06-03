namespace Dex.Cap.Outbox
{
    public interface IOutboxMessageProcessorFactory
    {
        IOutboxMessageProcessor GetMessageProcessor(Models.Outbox outbox);
    }
}