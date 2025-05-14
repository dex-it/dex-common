using System.Threading;
using System.Threading.Tasks;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed
{
    /// <summary>
    /// Event sender contract
    /// </summary>
    /// <typeparam name="TBus">Bus type</typeparam>
    public interface IDistributedEventRaiser<TBus>
        where TBus : IBus
    {
        /// <summary>
        /// Send event to the queue
        /// </summary>
        /// <param name="args">DistributedEvent</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        Task RaiseAsync<T>(T args, CancellationToken cancellationToken)
            where T : class;
    }
}