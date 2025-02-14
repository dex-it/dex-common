using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestDelayOutboxCommand : IOutboxMessage
    {
        public int DelayMsec { get; set; }
        public string Args { get; set; }
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}