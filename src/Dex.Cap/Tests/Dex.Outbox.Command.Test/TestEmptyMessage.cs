using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessage : IOutboxMessage
{
    public static string OutboxTypeId => string.Empty;
}