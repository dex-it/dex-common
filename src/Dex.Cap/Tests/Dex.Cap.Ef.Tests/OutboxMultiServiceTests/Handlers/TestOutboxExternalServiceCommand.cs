using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestOutboxExternalServiceCommand : BaseOutboxMessage
{
    public string Args { get; set; }
}