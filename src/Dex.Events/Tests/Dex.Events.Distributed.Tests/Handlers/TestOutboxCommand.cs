using Dex.Cap.Outbox.Models;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestOutboxCommand : BaseOutboxMessage
    {
        public string Args { get; set; }
    }
}