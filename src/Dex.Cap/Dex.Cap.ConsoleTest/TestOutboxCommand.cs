using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.ConsoleTest
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
    }
}
