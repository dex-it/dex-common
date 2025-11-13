using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestErrorOutboxCommand : IOutboxMessage
{
    public string OutboxTypeId => "4E377ABD-C0E2-463E-A622-BE306F42EB11";

    public int MaxCount { get; init; } = 1;
}