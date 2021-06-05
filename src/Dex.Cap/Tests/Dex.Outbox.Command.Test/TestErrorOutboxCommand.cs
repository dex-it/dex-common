using Dex.Cap.Outbox;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorOutboxCommand : IOutboxMessage
    {
        public int CountDown { get; set; } = 1;
    }
}