using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.DistributedCache.Services
{
    internal interface ICacheManagementService
    {
        string GenerateCacheKey(IDictionary<Type, string> variableKeys, IEnumerable<string> paramsList);
        Task<byte[]?> GetMetaInfoAsync(string key, CancellationToken cancellation);
        Task<byte[]?> GetValueDataAsync(string key, CancellationToken cancellation);
        Task SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation);
        Task SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation);

        Task SetCacheDependenciesAsync(string key, int expiration, IDictionary<Type, string> variableKeys,
            object? executedActionResult, CancellationToken cancellation);

        Task InvalidateByCacheKeyAsync(string key, CancellationToken cancellation);
    }
}