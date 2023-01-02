using Dex.Cap.Outbox.Models;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand2 : BaseOutboxMessage
    {
        public string Args { get; set; }
    }
}