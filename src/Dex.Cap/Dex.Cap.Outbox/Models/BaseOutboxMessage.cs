using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    public abstract class BaseOutboxMessage : IOutboxMessage
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}