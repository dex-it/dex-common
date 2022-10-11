using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;

namespace Dex.DistributedCache.Services
{
    public interface ICacheService
    {
        Task SetDependencyValueDataAsync(string key, IEnumerable<CachePartitionedDependencies> partDependencies, int expiration,
            CancellationToken cancellation);

        Task InvalidateByDependenciesAsync(IEnumerable<CachePartitionedDependencies> partDependencies, CancellationToken cancellation);

        // TODO зачем он ?
        Task InvalidateByVariableKeyAsync<T>(IEnumerable<string> values, CancellationToken cancellation) where T : ICacheVariableKey;
    }
}