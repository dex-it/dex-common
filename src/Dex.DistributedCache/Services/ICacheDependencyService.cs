using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.DistributedCache.Services
{
    public interface ICacheDependencyService
    {
        Task SetAsync(string key, Guid userId, object? valueData, int expiration, CancellationToken cancellation);
    }

    public interface ICacheDependencyService<in TValue> : ICacheDependencyService
    {
        Task SetAsync(string key, Guid userId, TValue? valueData, int expiration, CancellationToken cancellation);

        Task ICacheDependencyService.SetAsync(string key, Guid userId, object? valueData, int expiration, CancellationToken cancellation)
        {
            return SetAsync(key, userId, (TValue?)valueData, expiration, cancellation);
        }
    }
}