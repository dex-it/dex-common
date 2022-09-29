using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Events.Distributed.Models;

#pragma warning disable CA1711

namespace Dex.Events.Distributed
{
    /// <summary>
    /// DistributedEventHandler contract
    /// </summary>
    /// <typeparam name="T">DistributedBaseEventParams</typeparam>
    public interface IDistributedEventHandler<in T>
        where T : DistributedBaseEventParams
    {
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        Task ProcessAsync(T argument, CancellationToken cancellationToken);
    }
}