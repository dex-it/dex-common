using System;
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

namespace Dex.DistributedCache.Services
{
    internal sealed class CacheActionFilterService : ICacheActionFilterService
    {
        private readonly ILogger<CacheActionFilterService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ICacheUserVariableFactory _cacheUserVariableFactory;

        public CacheActionFilterService(ILogger<CacheActionFilterService> logger, ICacheService cacheService,
            ICacheUserVariableFactory cacheUserVariableFactory)
        {
            _logger = logger;
            _cacheService = cacheService;
            _cacheUserVariableFactory = cacheUserVariableFactory;
        }

        public Guid GetUserId(bool isUserVariableKey)
        {
            var userId = Guid.Empty;
            if (!isUserVariableKey) return userId;

            var userIdService = _cacheUserVariableFactory.GetCacheUserVariableService();
            userId = userIdService.UserId;

            return userId;
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

                    await _cacheService.SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, executedContext.HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                    await _cacheService.SetValueDataAsync(key, cacheValueByte, expiration, executedContext.HttpContext.RequestAborted).ConfigureAwait(false);
                    SetETagHeader(executedContext, cacheMetaInfo);

                    await _cacheService.SetCacheDependenciesAsync(key, expiration, userId, executedContext).ConfigureAwait(false);
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
                        await _cacheService.SetMetaInfoAsync(key, cacheMetaInfo.GetBytes(), expiration, executedContext.HttpContext.RequestAborted)
                            .ConfigureAwait(false);
                    }
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