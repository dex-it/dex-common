using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;

namespace Dex.Cap.Ef.Tests
{
    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add<TestUserCreatorCommand>(nameof(TestUserCreatorCommand));
            Add<TestOutboxCommand>(nameof(TestOutboxCommand));
            Add<TestOutboxCommand2>(nameof(TestOutboxCommand2));

            Add<TestErrorOutboxCommand>(nameof(TestErrorOutboxCommand));
            Add<TestDelayOutboxCommand>(nameof(TestDelayOutboxCommand));
        }
    }
}