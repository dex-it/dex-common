using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestOutboxExternalServiceCommand : IOutboxMessage
{
    public string Args { get; set; } = null!;
    public Guid MessageId { get; init; } = Guid.NewGuid();
}