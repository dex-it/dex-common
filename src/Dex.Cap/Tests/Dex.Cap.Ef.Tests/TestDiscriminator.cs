using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox;
using Dex.Outbox.Command.Test;

namespace Dex.Cap.Ef.Tests
{
    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add<TestUserCreatorCommand>("6262F3D6-498F-4820-B372-6C3425824CD9");
            Add<TestOutboxCommand>("15CAD1F5-4C0D-4816-B5D1-E2340144C4AA");
            Add<TestOutboxCommand2>("36003399-08FB-48E0-B52A-803883805DAA");

            Add<TestErrorOutboxCommand>("4E377ABD-C0E2-463E-A622-BE306F42EB11");
            Add<TestDelayOutboxCommand>("ECF5C0E2-4490-4D7E-A177-3D888CD6EA0D");
        }
    }
}