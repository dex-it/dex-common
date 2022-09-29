using System;

namespace Dex.Events.Distributed.Tests.Events
{
    public class OnCardAdded : DistributedBaseEventParams
    {
        public Guid CardId { get; set; }
    }
}