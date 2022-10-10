using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dex.DistributedCache.Services
{
    public interface ICacheService
    {
        Task SetDependencyValueDataAsync(string key, CachePartitionedDependencies[] partDependencies, int expiration, CancellationToken cancellation);

        Task InvalidateByDependenciesAsync(CachePartitionedDependencies[] partDependencies, CancellationToken cancellation);
        
        internal string GenerateCacheKey(Guid userId, ActionExecutingContext executingContext);

        internal Task<byte[]?> GetMetaInfoAsync(string key, CancellationToken cancellation);

        internal Task<byte[]?> GetValueDataAsync(string key, CancellationToken cancellation);

        internal Task SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation);

        internal Task SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation);

        internal Task SetCacheDependenciesAsync(string key, int expiration, Guid userId, ActionExecutedContext executedContext);

        internal Task InvalidateByCacheKeyAsync(string key, CancellationToken cancellation);
    }
}