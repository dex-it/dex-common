using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    public string OutboxTypeId => nameof(TestEmptyMessageId);
}