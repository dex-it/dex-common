using Dex.Cap.Outbox.Interfaces;

namespace Dex.Events.DistributedEvents.Tests
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
    }
}