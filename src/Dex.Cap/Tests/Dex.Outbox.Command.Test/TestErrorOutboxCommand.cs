using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorOutboxCommand : IOutboxMessage
    {
        public int MaxCount { get; set; } = 1;
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}