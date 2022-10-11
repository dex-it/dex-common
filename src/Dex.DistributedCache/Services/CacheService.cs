using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Helpers;
using Dex.DistributedCache.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Dex.DistributedCache.Services
{
    public sealed class CacheService : ICacheService, ICacheManagementService
    {
        private const string KeyPrefix = "dc";
        private const string KeyMetaInfoPrefix = "meta";
        private const string KeyValuePrefix = "value";
        private const char KeySeparator = ':';

        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly ICacheDependencyFactory _cacheDependencyFactory;

        public CacheService(IDistributedCache cache, ICacheDependencyFactory cacheDependencyFactory, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _cacheDependencyFactory = cacheDependencyFactory;
        }

        public async Task SetDependencyValueDataAsync(string key, IEnumerable<CachePartitionedDependencies> partDependencies,
            int expiration, CancellationToken cancellation)
        {
            if (partDependencies == null) throw new ArgumentNullException(nameof(partDependencies));

            foreach (var partDependency in partDependencies)
            {
                foreach (var partDependencyValue in partDependency.Values)
                {
                    var cacheByte = await GetValueDataByDependencyAsync(partDependencyValue, partDependency.Type, cancellation).ConfigureAwait(false);
                    var dependencyValueKeys = new List<string>();

                    if (cacheByte != null)
                    {
                        dependencyValueKeys = JsonSerializer.Deserialize<List<string>>(cacheByte);

                        if (dependencyValueKeys == null)
                        {
                            _logger.LogWarning("Unable to deserialize: {Data}", Encoding.UTF8.GetString(cacheByte));
                            dependencyValueKeys = new List<string>();
                        }
                    }

                    if (!dependencyValueKeys.Contains(key))
                    {
                        dependencyValueKeys.Add(key);
                    }

                    await _cache.SetAsync(GetCacheKeyForDependencyValueData(partDependencyValue, partDependency.Type),
                            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dependencyValueKeys)), GetCacheEntryOptions(expiration), cancellation)
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task InvalidateByDependenciesAsync(IEnumerable<CachePartitionedDependencies> partDependencies, CancellationToken cancellation)
        {
            if (partDependencies == null) throw new ArgumentNullException(nameof(partDependencies));

            foreach (var partDependency in partDependencies)
            foreach (var partDependencyValue in partDependency.Values)
            {
                var cacheByte = await GetValueDataByDependencyAsync(partDependencyValue, partDependency.Type, cancellation).ConfigureAwait(false);
                var dependencyValueKeys = new List<string>();

                if (cacheByte != null)
                {
                    dependencyValueKeys = JsonSerializer.Deserialize<List<string>>(cacheByte);

                    if (dependencyValueKeys == null)
                    {
                        _logger.LogWarning("Unable to deserialize: {Data}", Encoding.UTF8.GetString(cacheByte));
                        dependencyValueKeys = new List<string>();
                    }
                }

                foreach (var key in dependencyValueKeys)
                {
                    await ((ICacheManagementService)this).InvalidateByCacheKeyAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        public async Task InvalidateByVariableKeyAsync<T>(IEnumerable<string> values, CancellationToken cancellation) where T : ICacheVariableKey
        {
            await InvalidateByDependenciesAsync(new[] { new CachePartitionedDependencies(typeof(T).Name, values.ToArray()) }, cancellation)
                .ConfigureAwait(false);
        }

        #region ICacheManagementService

        // TODO check performance
        string ICacheManagementService.GenerateCacheKey(IDictionary<Type, string> variableKeys, IEnumerable<string> paramsList)
        {
            return GenerateCacheKeyByParams(paramsList.Concat(variableKeys.Select(vk => vk.Key.Name + KeySeparator + vk.Value)));
        }

        async Task<byte[]?> ICacheManagementService.GetMetaInfoAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
        }

        async Task<byte[]?> ICacheManagementService.GetValueDataAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        async Task ICacheManagementService.SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForMetaInfo(key), metaInfo, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        async Task ICacheManagementService.SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForValueData(key), valueData, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        async Task ICacheManagementService.InvalidateByCacheKeyAsync(string key, CancellationToken cancellation)
        {
            await _cache.RemoveAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
            await _cache.RemoveAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        async Task ICacheManagementService.SetCacheDependenciesAsync(string key, int expiration,
            IDictionary<Type, string> variableKeys, object? executedActionResult, CancellationToken cancellation)
        {
            if (variableKeys.Count > 0)
            {
                var partDependencies = variableKeys.Select(vk => new CachePartitionedDependencies(vk.Key.Name, new[] { vk.Value }));
                await SetDependencyValueDataAsync(key, partDependencies, expiration, cancellation).ConfigureAwait(false);
            }

            var resultType = executedActionResult?.GetType();
            if (resultType != null)
            {
                var cacheDependencyService = _cacheDependencyFactory.GetCacheDependencyService(resultType);
                if (cacheDependencyService != null)
                {
                    await cacheDependencyService.SetAsync(key, executedActionResult, expiration, cancellation).ConfigureAwait(false);
                }
            }
        }

        #endregion

        private static string GenerateCacheKeyByParams(IEnumerable<string> args)
        {
            var argsStr = string.Join(KeySeparator, args);
            // TODO optimize, memory alloc, encoding
            return CacheHelper.CreateMd5(CacheHelper.Base64Encode(argsStr));
        }

        private static DistributedCacheEntryOptions GetCacheEntryOptions(int expiration)
        {
            return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiration) };
        }

        private async Task<byte[]?> GetValueDataByDependencyAsync(string key, string dependencyType, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForDependencyValueData(key, dependencyType), cancellation).ConfigureAwait(false);
        }

        private static string GetCacheKeyForMetaInfo(string key)
        {
            var keyArgs = new[] { KeyPrefix, KeyMetaInfoPrefix, key };
            return string.Join(KeySeparator, keyArgs);
        }

        private static string GetCacheKeyForValueData(string key)
        {
            var keyArgs = new[] { KeyPrefix, KeyValuePrefix, key };
            return string.Join(KeySeparator, keyArgs);
        }

        private static string GetCacheKeyForDependencyValueData(string key, string dependencyType)
        {
            return string.Join(KeySeparator, KeyPrefix, dependencyType, key);
        }
    }
}