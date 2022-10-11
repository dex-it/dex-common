using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;

namespace Dex.DistributedCache.Services
{
    public interface ICacheService
    {
        Task SetDependencyValueDataAsync(string key, CachePartitionedDependencies[] partDependencies, int expiration, CancellationToken cancellation);

        Task InvalidateByDependenciesAsync(CachePartitionedDependencies[] partDependencies, CancellationToken cancellation);

        Task InvalidateByVariableKeyAsync<T>(string[] values, CancellationToken cancellation)
            where T : ICacheVariableKey;

        internal string GenerateCacheKey(Dictionary<Type, string> variableKeys, List<string> paramsList);

        internal Task<byte[]?> GetMetaInfoAsync(string key, CancellationToken cancellation);

        internal Task<byte[]?> GetValueDataAsync(string key, CancellationToken cancellation);

        internal Task SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation);

        internal Task SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation);

        internal Task SetCacheDependenciesAsync(string key, int expiration, Dictionary<Type, string> variableKeys, object? executedActionResult,
            CancellationToken cancellation);

        internal Task InvalidateByCacheKeyAsync(string key, CancellationToken cancellation);
    }
}