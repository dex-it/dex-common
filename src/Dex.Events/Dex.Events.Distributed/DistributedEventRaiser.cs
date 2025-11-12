using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.Events.Distributed;

internal sealed class DistributedEventRaiser<TBus> : IDistributedEventRaiser<TBus>
    where TBus : IBus
{
    private readonly TBus _bus;

    public DistributedEventRaiser(TBus bus)
    {
        _bus = bus;
    }

    public Task RaiseAsync<T>(T args, CancellationToken cancellationToken)
        where T : class
    {
        // The Publish(T) and Publish(object) work differently:
        // - in the first method, the type is defined by typeof(T)
        // - in the second method, the type is defined by GetType()
        return _bus.Publish(args as object, cancellationToken);
    }
}