using Dex.Cap.Outbox.Models;

namespace Dex.Outbox.Command.Test
{
    public class TestDelayOutboxCommand : BaseOutboxMessage
    {
        public int DelayMsec { get; set; }
        public string Args { get; set; }
    }
}