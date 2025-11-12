using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.Tests.Models;

namespace Dex.Events.Distributed.Tests.Events
{
    public sealed class OnUserAdded : DistributedCustomerEventParams, IOutboxMessage
    {
        public string OutboxTypeId => nameof(OnUserAdded);
    }
}