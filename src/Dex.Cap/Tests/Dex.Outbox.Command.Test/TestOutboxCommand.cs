using Dex.Cap.Outbox.Models;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand : BaseOutboxMessage
    {
        public string Args { get; set; }
    }
}