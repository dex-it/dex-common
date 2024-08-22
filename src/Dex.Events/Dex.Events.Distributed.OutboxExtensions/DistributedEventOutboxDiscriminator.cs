using Dex.Cap.Outbox;
using MassTransit;

namespace Dex.Events.Distributed.OutboxExtensions
{
    public class DistributedEventOutboxDiscriminator : BaseOutboxTypeDiscriminator
    {
        /// <summary>
        /// Discriminator constructor (mapping a type to a random identifier)
        /// </summary>
        public DistributedEventOutboxDiscriminator()
        {
            Add<OutboxDistributedEventMessage<IBus>>("36A8B4B2-C5EB-41DE-B6DB-9732CDE55555");
        }
    }

    public class DistributedEventOutboxDiscriminator<TBus> : DistributedEventOutboxDiscriminator
        where TBus : IBus
    {
        /// <summary>
        /// Discriminator constructor (mapping a type to a random identifier)
        /// </summary>
        public DistributedEventOutboxDiscriminator()
        {
            if (typeof(TBus) != typeof(IBus))
            {
                Add<OutboxDistributedEventMessage<TBus>>("EF92FB62-DC00-49CB-A042-FDE816BFF20C");
            }
        }
    }
}