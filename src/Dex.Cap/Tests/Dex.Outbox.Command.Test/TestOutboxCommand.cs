using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestOutboxCommand : IOutboxMessage
{
    public static string OutboxTypeId => "15CAD1F5-4C0D-4816-B5D1-E2340144C4AA";

    public string Args { get; init; }

    public Guid TestId { get; init; } = Guid.NewGuid();
}