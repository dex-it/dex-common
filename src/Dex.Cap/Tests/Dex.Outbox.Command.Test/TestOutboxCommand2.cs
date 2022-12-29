using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand2 : IOutboxMessage
    {
        public string Args { get; set; }
        public Guid MessageId { get; } = Guid.NewGuid();
    }
}