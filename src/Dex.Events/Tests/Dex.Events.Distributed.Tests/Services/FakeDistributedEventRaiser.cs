using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.Events.Distributed.Tests.Services
{
    public sealed class FakeDistributedEventRaiser<TBus> : IDistributedEventRaiser<TBus>
        where TBus : IBus
    {
        public Task RaiseAsync<T>(T args, CancellationToken cancellationToken)
            where T : DistributedBaseEventParams
        {
            return Task.CompletedTask;
        }
    }
}