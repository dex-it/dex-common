using Dex.Cap.Outbox;
using Dex.Events.Distributed.OutboxExtensions;
using Dex.Events.Distributed.Tests.Handlers;
using Dex.Events.Distributed.Tests.Services;
using MassTransit;

namespace Dex.Events.Distributed.Tests.Tests
{
    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add<TestOutboxCommand>(nameof(TestOutboxCommand));
            Add<OutboxDistributedEventMessage<IBus>>("OutboxDistributedEventMessage_IBus");
            Add<OutboxDistributedEventMessage<IExternalBus>>("OutboxDistributedEventMessage_IExternalBus");
        }
    }
}