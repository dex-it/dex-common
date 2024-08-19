using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.Events.Distributed
{
    internal sealed class DistributedEventRaiser<TBus> : IDistributedEventRaiser<TBus>
        where TBus : IBus
    {
        public TBus Bus { get; }

        public DistributedEventRaiser(TBus bus)
        {
            Bus = bus;
        }

        public async Task RaiseAsync<T>(T args, CancellationToken cancellationToken)
            where T : class, IDistributedEventParams
        {
            // The Publish(T) and Publish(object) work differently:
            // - in the first method, the type is defined by typeof(T)
            // - in the second method, the type is defined by GetType()
            await Bus.Publish(args as object, cancellationToken).ConfigureAwait(false);
        }
    }
}