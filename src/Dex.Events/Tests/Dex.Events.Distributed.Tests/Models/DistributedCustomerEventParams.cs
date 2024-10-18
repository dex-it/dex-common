using System;

namespace Dex.Events.Distributed.Tests.Models
{
    public class DistributedCustomerEventParams : IDistributedEventParams
    {
        public Guid CustomerId { get; set; }
    }
}