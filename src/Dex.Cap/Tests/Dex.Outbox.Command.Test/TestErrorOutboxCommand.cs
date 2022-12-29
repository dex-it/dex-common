using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorOutboxCommand : IOutboxMessage
    {
        public int MaxCount { get; set; } = 1;
        public Guid MessageId { get; } = Guid.NewGuid();
    }
}