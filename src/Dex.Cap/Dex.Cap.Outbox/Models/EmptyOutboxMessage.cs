using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    internal sealed class EmptyOutboxMessage : IOutboxMessage
    {
        public static readonly EmptyOutboxMessage Empty = new();
        public Guid MessageId => Guid.Empty;
    }
}