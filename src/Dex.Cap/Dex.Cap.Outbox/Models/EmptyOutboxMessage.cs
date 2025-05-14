namespace Dex.Cap.Outbox.Models
{
    internal sealed class EmptyOutboxMessage
    {
        public static readonly EmptyOutboxMessage Empty = new();
    }
}