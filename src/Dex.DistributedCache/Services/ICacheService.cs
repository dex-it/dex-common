using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dex.DistributedCache.Services
{
    public interface ICacheService
    {
        string GenerateCacheKey(Guid userId, ActionExecutingContext executingContext);

        Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext);

        Task TryCacheValue(string key, int expiration, Guid userId, ActionExecutedContext executedContext);

        Task SetDependencyValueDataAsync(string key, CachePartitionedDependencies[] partDependencies, int expiration, CancellationToken cancellation);

        Task InvalidateByDependenciesAsync(CachePartitionedDependencies[] partDependencies, CancellationToken cancellation);
    }
}