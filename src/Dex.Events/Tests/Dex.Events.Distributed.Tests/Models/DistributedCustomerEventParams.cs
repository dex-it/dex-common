using System;
using Dex.Events.Distributed.Models;

namespace Dex.Events.Distributed.Tests.Models
{
    public class DistributedCustomerEventParams : DistributedBaseEventParams
    {
        public Guid CustomerId { get; set; }
    }
}