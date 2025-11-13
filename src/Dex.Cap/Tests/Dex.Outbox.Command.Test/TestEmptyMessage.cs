using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    public string OutboxTypeId => nameof(TestEmptyMessageId);
}