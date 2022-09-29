using Dex.Cap.Outbox.Interfaces;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
    }
}