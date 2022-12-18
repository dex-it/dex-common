using System.Threading;
using System.Threading.Tasks;

namespace Dex.DistributedCache.Services
{
    public interface ICacheDependencyService
    {
        Task SetAsync(string key, object? valueData, int expiration, CancellationToken cancellation);
    }

    public interface ICacheDependencyService<in TValue> : ICacheDependencyService
    {
        Task SetAsync(string key, TValue? valueData, int expiration, CancellationToken cancellation);

        Task ICacheDependencyService.SetAsync(string key, object? valueData, int expiration, CancellationToken cancellation)
        {
            return SetAsync(key, (TValue?)valueData, expiration, cancellation);
        }
    }
}