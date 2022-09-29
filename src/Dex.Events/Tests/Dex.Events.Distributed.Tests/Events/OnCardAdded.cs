using System;
using Dex.Events.Distributed.Models;

namespace Dex.Events.Distributed.Tests.Events
{
    public sealed class OnCardAdded : DistributedCustomerEventParams
    {
        public Guid CardId { get; set; }
    }
}