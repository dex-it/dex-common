using Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;
using Dex.Cap.Outbox;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Discriminators;

public class TestExternalServiceDiscriminator : BaseOutboxTypeDiscriminator
{
    public TestExternalServiceDiscriminator()
    {
        Add<TestOutboxExternalServiceCommand>("8E083706-66EA-4E6B-8E5C-23F99AF9A01D");
    }
}