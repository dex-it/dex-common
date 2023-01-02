using System;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessage
    {
        Guid MessageId { get; }
    }
}