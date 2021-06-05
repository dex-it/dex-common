using Dex.Cap.Outbox;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
    }
}