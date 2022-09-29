using System;
using MassTransit;

namespace Dex.Events.Distributed
{
    /// <summary>
    /// Base events params
    /// </summary>
    public abstract class DistributedBaseEventParams : IConsumer
    {
        public Guid CustomerId { get; set; }
    }
}