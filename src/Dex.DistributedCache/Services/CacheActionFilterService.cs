using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

#pragma warning disable CA1031

namespace Dex.DistributedCache.Services
{
    internal sealed class CacheActionFilterService : ICacheActionFilterService
    {
        private readonly ILogger<CacheActionFilterService> _logger;
        private readonly ICacheManagementService _cacheService;
        private readonly ICacheVariableKeyFactory _cacheVariableKeyFactory;

        public CacheActionFilterService(ILogger<CacheActionFilterService> logger, ICacheManagementService cacheService,
            ICacheVariableKeyFactory cacheVariableKeyFactory)
        {
            _logger = logger;
            _cacheService = cacheService;
            _cacheVariableKeyFactory = cacheVariableKeyFactory;
        }

        public IDictionary<Type, string> GetVariableKeys(IEnumerable<Type> cacheVariableKeys)
        {
            var variableKeyDictionary = new Dictionary<Type, string>();
            foreach (var cacheVariableKey in cacheVariableKeys)
            {
                var cacheVariableKeyService = _cacheVariableKeyFactory.GetCacheVariableKeyService(cacheVariableKey);
                if (cacheVariableKeyService == null) continue;

                var variableKey = cacheVariableKeyService.GetVariableKey();
                variableKeyDictionary.Add(cacheVariableKey, variableKey);
            }

            return variableKeyDictionary;
        }

        public async Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext)
        {
            var request = executingContext.HttpContext.Request;
            var cacheMetaInfoByte = await _cacheService.GetMetaInfoAsync(key, executingContext.HttpContext.RequestAborted).ConfigureAwait(false);
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

                var cacheValueByte = await _cacheService.GetValueDataAsync(key, executingContext.HttpContext.RequestAborted).ConfigureAwait(false);
                if (cacheValueByte == null) return false;

                SetResponseData(executingContext, cacheMetaInfo, cacheValueByte);
                return true;
            }

            return false;
        }

        public async Task<bool> TryCacheValue(string key, int expiration, IDictionary<Type, string> variableKeys, ActionExecutedContext executedContext)
        {
            if (executedContext.Exception != null) throw executedContext.Exception;

            var cancellation = executedContext.HttpContext.RequestAborted;
            CacheMetaInfo? cacheMetaInfo = default;
            var isError = false;

            try
            {
                var cacheValueByte = await GetBytesFromResponseAsync(executedContext).ConfigureAwait(false);
                var response = executedContext.HttpContext.Response;

                if (response.StatusCode == (int)HttpStatusCode.OK)
                {
                    cacheMetaInfo = new CacheMetaInfo(CacheHelper.GenerateETag(), response.ContentType);

                    await _cacheService.SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, cancellation).ConfigureAwait(false);
                    await _cacheService.SetValueDataAsync(key, cacheValueByte, expiration, cancellation).ConfigureAwait(false);
                    SetETagHeader(executedContext, cacheMetaInfo);

                    var executedActionResult = (executedContext.Result as ObjectResult)?.Value;
                    await _cacheService.SetCacheDependenciesAsync(key, expiration, variableKeys, executedActionResult, cancellation).ConfigureAwait(false);
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
                    await _cacheService.InvalidateByCacheKeyAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    if (cacheMetaInfo != null)
                    {
                        cacheMetaInfo.CompleteCache();
                        await _cacheService.SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, cancellation).ConfigureAwait(false);
                    }
                }
            }

            return !isError;
        }

        [SuppressMessage("Reliability", "CA2007:Попробуйте вызвать ConfigureAwait для ожидаемой задачи")]
        private static async Task<byte[]> GetBytesFromResponseAsync(ActionExecutedContext executedContext)
        {
            if (executedContext.Result == null) throw new ArgumentNullException(nameof(executedContext));

            var originBody = executedContext.HttpContext.Response.Body;
            await using var buffer = new MemoryStream();
            executedContext.HttpContext.Response.Body = buffer;
            await executedContext.Result.ExecuteResultAsync(executedContext).ConfigureAwait(false);
            await buffer.CopyToAsync(originBody, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
            executedContext.HttpContext.Response.Body = originBody;

            return buffer.ToArray();
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
    }
}