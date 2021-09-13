using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorOutboxCommand : IOutboxMessage
    {
        public int CountDown { get; set; } = 1;
    }
}