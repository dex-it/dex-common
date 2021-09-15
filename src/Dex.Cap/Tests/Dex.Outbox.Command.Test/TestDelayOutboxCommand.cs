using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestDelayOutboxCommand : IOutboxMessage
    {
        public int DelayMsec { get; set; }
        public string Args { get; set; }
    }
}
