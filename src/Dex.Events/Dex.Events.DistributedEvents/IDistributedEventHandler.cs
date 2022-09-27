using System.Threading;
using System.Threading.Tasks;

namespace DistributedEvents
{
    /// <summary>
    /// DistributedEventHandler contract
    /// </summary>
    /// <typeparam name="T">DistributedBaseEventParams</typeparam>
    public interface IDistributedEventHandler<in T>
        where T : DistributedBaseEventParams
    {
        Task ProcessAsync(T argument, CancellationToken cancellationToken);
    }
}