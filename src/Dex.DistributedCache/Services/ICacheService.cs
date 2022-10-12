using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;

namespace Dex.DistributedCache.Services
{
    public interface ICacheService
    {
        Task SetDependencyValueDataAsync(string key, IEnumerable<CacheDependency> dependencies, int expiration, CancellationToken cancellation);

        Task InvalidateByDependenciesAsync(IEnumerable<CacheDependency> dependencies, CancellationToken cancellation);
    }
}