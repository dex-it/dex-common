using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}