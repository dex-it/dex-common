namespace Dex.Cap.Outbox.Models
{
    public enum OutboxMessageStatus
    {
        New = 0,
        Failed = 1,
        Succeeded = 2
    }
}