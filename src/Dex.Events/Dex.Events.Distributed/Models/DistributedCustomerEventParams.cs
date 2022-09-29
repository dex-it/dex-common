using System;

namespace Dex.Events.Distributed.Models
{
    public class DistributedCustomerEventParams : DistributedBaseEventParams
    {
        public Guid CustomerId { get; set; }
    }
}