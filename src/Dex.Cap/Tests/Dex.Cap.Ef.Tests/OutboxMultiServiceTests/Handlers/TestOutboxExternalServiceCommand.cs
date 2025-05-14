using System;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestOutboxExternalServiceCommand
{
    public string Args { get; set; } = null!;
    public Guid TestId { get; init; } = Guid.NewGuid();
}