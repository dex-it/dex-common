using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
using Microsoft.Net.Http.Headers;

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

        public string GenerateCacheKey(Guid userId, ActionExecutingContext executingContext)
        {
            var request = executingContext.HttpContext.Request;
            var paramsList = new List<string> { CacheHelper.GetDisplayUrl(request) };

            if (userId != Guid.Empty)
            {
                paramsList.Add(userId.ToString());
            }

            return GenerateCacheKeyByParams(paramsList.ToArray());
        }

        public async Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext)
        {
            var request = executingContext.HttpContext.Request;
            var cacheMetaInfoByte = await GetMetaInfoAsync(key, executingContext.HttpContext.RequestAborted).ConfigureAwait(false);
            if (cacheMetaInfoByte == null) return false;

            var cacheMetaInfo = JsonSerializer.Deserialize<CacheMetaInfo>(cacheMetaInfoByte);
            if (cacheMetaInfo == null)
            {
                _logger.LogWarning("Unable to deserialize CacheMetaInfo: {CacheMetaInfo}", Encoding.UTF8.GetString(cacheMetaInfoByte));
            }
            else
            {
                if (!cacheMetaInfo.IsCompleted) return false;

                if (request.Headers.ContainsKey(HeaderNames.IfNoneMatch))
                {
                    var incomingETag = request.Headers[HeaderNames.IfNoneMatch].ToString();
                    if (incomingETag.Equals(cacheMetaInfo.ETag, StringComparison.Ordinal))
                    {
                        executingContext.Result = new StatusCodeResult((int)HttpStatusCode.NotModified);
                        return true;
                    }
                }

                var cacheValueByte = await GetValueDataAsync(key, executingContext.HttpContext.RequestAborted).ConfigureAwait(false);
                if (cacheValueByte == null) return false;

                SetResponseData(executingContext, cacheMetaInfo, cacheValueByte);
                return true;
            }

            return false;
        }

        public async Task TryCacheValue(string key, int expiration, Guid userId, ActionExecutedContext executedContext)
        {
            if (executedContext.Exception != null) throw executedContext.Exception;

            CacheMetaInfo? cacheMetaInfo = default;
            var isError = false;

            try
            {
                var cacheValueByte = await GetBytesFromResponseAsync(executedContext).ConfigureAwait(false);
                var response = executedContext.HttpContext.Response;

                if (response.StatusCode == (int)HttpStatusCode.OK)
                {
                    cacheMetaInfo = new CacheMetaInfo(CacheHelper.GenerateETag(), response.ContentType);

                    await SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
                    await SetValueDataAsync(key, cacheValueByte, expiration, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
                    SetETagHeader(executedContext, cacheMetaInfo);

                    await SetCacheDependenciesAsync(key, expiration, userId, executedContext).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                isError = true;
                if (e is ObjectDisposedException or OperationCanceledException)
                {
                    // Ignore
                }
                else
                {
                    _logger.LogWarning(e, "Set cache error");
                }
            }
            finally
            {
                if (isError)
                {
                    await InvalidateByCacheKeyAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    if (cacheMetaInfo != null)
                    {
                        cacheMetaInfo.CompleteCache();
                        await SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
                    }
                }
            }
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
                    await InvalidateByCacheKeyAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private static string GenerateCacheKeyByParams(params string[] args)
        {
            var argsStr = string.Join(KeySeparator, args);
            return CacheHelper.CreateMd5(CacheHelper.Base64Encode($"{argsStr}"));
        }

        private async Task<byte[]?> GetMetaInfoAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
        }

        private async Task<byte[]?> GetValueDataAsync(string key, CancellationToken cancellation)
        {
            return await _cache.GetAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        private async Task SetMetaInfoAsync(string key, byte[]? metaInfo, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForMetaInfo(key), metaInfo, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        private async Task SetValueDataAsync(string key, byte[]? valueData, int expiration, CancellationToken cancellation)
        {
            await _cache.SetAsync(GetCacheKeyForValueData(key), valueData, GetCacheEntryOptions(expiration), cancellation).ConfigureAwait(false);
        }

        private async Task InvalidateByCacheKeyAsync(string key, CancellationToken cancellation = default)
        {
            await _cache.RemoveAsync(GetCacheKeyForMetaInfo(key), cancellation).ConfigureAwait(false);
            await _cache.RemoveAsync(GetCacheKeyForValueData(key), cancellation).ConfigureAwait(false);
        }

        private static void SetResponseData(ActionExecutingContext context, CacheMetaInfo cacheMetaInfo, byte[] cacheValueByte)
        {
            SetETagHeader(context, cacheMetaInfo);
            context.Result = new FileContentResult(cacheValueByte, cacheMetaInfo.ContentType);
        }

        private static void SetETagHeader(ActionContext context, CacheMetaInfo cacheMetaInfo)
        {
            context.HttpContext.Response.Headers.Add(HeaderNames.ETag, new[] { cacheMetaInfo.ETag });
        }

        private async Task SetCacheDependenciesAsync(string key, int expiration, Guid userId, ActionExecutedContext executedContext)
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

        private static async Task<byte[]> GetBytesFromResponseAsync(ActionExecutedContext executedContext)
        {
            if (executedContext.Result == null) throw new ArgumentNullException(nameof(executedContext.Result));

            var originBody = executedContext.HttpContext.Response.Body;
            await using var buffer = new MemoryStream();
            executedContext.HttpContext.Response.Body = buffer;
            await executedContext.Result.ExecuteResultAsync(executedContext).ConfigureAwait(false);
            await buffer.CopyToAsync(originBody, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
            executedContext.HttpContext.Response.Body = originBody;

            return buffer.ToArray();
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