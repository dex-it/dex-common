using System;
using DistributedEvents;

namespace Dex.Events.DistributedEvents.Tests.Events
{
    public class OnCardAdded : DistributedBaseEventParams
    {
        public Guid CardId { get; set; }
    }
}