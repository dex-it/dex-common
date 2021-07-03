using Dex.Cap.Outbox;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand2 : IOutboxMessage
    {
        public string Args { get; set; }
    }
}