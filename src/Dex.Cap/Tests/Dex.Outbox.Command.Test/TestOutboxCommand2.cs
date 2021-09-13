using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand2 : IOutboxMessage
    {
        public string Args { get; set; }
    }
}