namespace Dex.Cap.Common.Interfaces
{
    public interface IOutboxMessage
    {
        Guid MessageId { get; }
    }
}