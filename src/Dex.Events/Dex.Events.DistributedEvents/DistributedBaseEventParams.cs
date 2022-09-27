using System;
using MassTransit;

namespace DistributedEvents
{
    /// <summary>
    /// Base events params
    /// </summary>
    public abstract class DistributedBaseEventParams : IConsumer
    {
        public Guid CustomerId { get; set; }
    }
}