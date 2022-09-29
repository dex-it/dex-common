using MassTransit;

namespace Dex.Events.Distributed.Models
{
    /// <summary>
    /// Base events params
    /// </summary>
    public abstract class DistributedBaseEventParams : IConsumer
    {
    }
}