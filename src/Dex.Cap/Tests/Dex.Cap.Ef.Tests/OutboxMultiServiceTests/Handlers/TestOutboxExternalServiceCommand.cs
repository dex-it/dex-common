using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestOutboxExternalServiceCommand : IOutboxMessage
{
    public string OutboxTypeId => "8E083706-66EA-4E6B-8E5C-23F99AF9A01D";

    public string? Args { get; init; }

    public Guid TestId { get; init; } = Guid.NewGuid();
}