using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.SecurityToken.DistributedStorage
{
    internal interface IDistributedCacheTypedClient
    {
        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default);

        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    }
}