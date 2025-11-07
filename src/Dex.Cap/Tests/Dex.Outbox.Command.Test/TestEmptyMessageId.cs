using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    public static string OutboxMessageType => null!;
}