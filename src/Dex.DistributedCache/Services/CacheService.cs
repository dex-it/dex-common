using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Helpers;
using Dex.DistributedCache.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Dex.DistributedCache.Services
{
    public sealed class CacheService : ICacheService
    {
        private const string KeyPrefix = "dc";
        private const string KeyMetaInfoPrefix = "meta";
        private const string KeyValuePrefix = "value";
        private const char KeySeparator = ':';

        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly ICacheDependencyFactory _cacheDependencyFactory;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger, ICacheDependencyFactory cacheDependencyFactory)
        {
            _cache = cache;
            _logger = logger;
            _cacheDependencyFactory = cacheDependencyFactory;
        }

        string ICacheService.GenerateCacheKey(Guid userId, ActionExecutingContext executingContext)
        {
            var request = executingContext.HttpContext.Request;
            var paramsList = new List<string> { CacheHelper.GetDisplayUrl(request) };

            if (userId != Guid.Empty)
            {
                paramsList.Add(userId.ToString());
            }

            return GenerateCacheKeyByParams(paramsList.ToArray());
        }

        public async Task SetDependencyValueDataAsync(string key, CachePartitionedDependencies[] partDependencies, int expiration,
            CancellationToken cancellation)
        {
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

        public async Task InvalidateByDependenciesAsync(CachePartitionedDependencies[] partDependencies, CancellationToken cancellation)
        {
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
                    await ((ICacheService)this).InvalidateByCacheKeyAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        async Task<byte[]?> ICacheService.GetMetaInfoAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
        }

        async Task<byte[]?> ICacheService.GetValueDataAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        async Task ICacheService.SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForMetaInfo(key), metaInfo, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        async Task ICacheService.SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForValueData(key), valueData, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        async Task ICacheService.InvalidateByCacheKeyAsync(string key, CancellationToken cancellation)
        {
            await _cache.RemoveAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
            await _cache.RemoveAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        async Task ICacheService.SetCacheDependenciesAsync(string key, int expiration, Guid userId, ActionExecutedContext executedContext)
        {
            var result = (executedContext.Result as ObjectResult)?.Value;
            var resultType = result?.GetType();
            if (resultType != null)
            {
                var cacheDependencyService = _cacheDependencyFactory.GetCacheDependencyService(resultType);
                if (cacheDependencyService != null)
                {
                    await cacheDependencyService.SetAsync(key, userId, result, expiration, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
                }
            }
        }

        private static string GenerateCacheKeyByParams(params string[] args)
        {
            var argsStr = string.Join(KeySeparator, args);
            return CacheHelper.CreateMd5(CacheHelper.Base64Encode($"{argsStr}"));
        }

        private static DistributedCacheEntryOptions GetCacheEntryOptions(int expiration)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiration) };
            return cacheEntryOptions;
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