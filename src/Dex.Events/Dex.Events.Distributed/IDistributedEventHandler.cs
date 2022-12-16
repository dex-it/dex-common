using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Events.Distributed.Models;
using MassTransit;

#pragma warning disable CA1711

namespace Dex.Events.Distributed
{
    /// <summary>
    /// DistributedEventHandler contract
    /// </summary>
    /// <typeparam name="T">DistributedBaseEventParams</typeparam>
    public interface IDistributedEventHandler<in T> : IConsumer<T>, IDistributedEventHandler
        where T : DistributedBaseEventParams
    {
    }

    [SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
    public interface IDistributedEventHandler : IConsumer
    {
        
    }
}