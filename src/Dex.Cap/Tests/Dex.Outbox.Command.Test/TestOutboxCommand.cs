using System;

namespace Dex.Outbox.Command.Test
{
    public class TestOutboxCommand
    {
        public string Args { get; set; }
        public Guid TestId { get; init; } = Guid.NewGuid();
    }
}