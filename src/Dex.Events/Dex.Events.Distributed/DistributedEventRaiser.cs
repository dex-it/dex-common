using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.Events.Distributed;

internal sealed class DistributedEventRaiser<TBus>(TBus bus) : IDistributedEventRaiser<TBus>
    where TBus : IBus
{
    public Task RaiseAsync<T>(T args, CancellationToken cancellationToken)
        where T : class
    {
        // The Publish(T) and Publish(object) work differently:
        // - in the first method, the type is defined by typeof(T)
        // - in the second method, the type is defined by GetType()
        return bus.Publish(args as object, cancellationToken);
    }
}