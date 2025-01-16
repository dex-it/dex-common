using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    public abstract class BaseOutboxMessage : IOutboxMessage, IIdempotentKey
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public string IdempotentKey => MessageId.ToString("N");
    }
}