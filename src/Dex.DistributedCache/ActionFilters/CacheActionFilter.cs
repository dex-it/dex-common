using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dex.DistributedCache.Helpers;
using Dex.DistributedCache.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.DistributedCache.ActionFilters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class CacheActionFilter : ActionFilterAttribute
    {
        public int Expiration { get; }
        public Type[] CacheVariableKeys { get; }

        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// CacheActionFilter
        /// </summary>
        /// <param name="expiration">Absolute expiration time in seconds, relative to now</param>
        /// <param name="cacheVariableKeys">Variable key interfaces</param>
        public CacheActionFilter(int expiration = 3600, params Type[] cacheVariableKeys)
        {
            Expiration = expiration;
            CacheVariableKeys = cacheVariableKeys;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));

            _serviceProvider = context.HttpContext.RequestServices;
            var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
            var cacheActionFilterService = _serviceProvider.GetRequiredService<ICacheActionFilterService>();

            var variableKeys = cacheActionFilterService.GetVariableKeys(CacheVariableKeys);
            var paramsList = new List<string> { CacheHelper.GetDisplayUrl(context.HttpContext.Request) };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            if (await cacheActionFilterService.CheckExistingCacheValue(cacheKey, context).ConfigureAwait(false)) return;

            var executedContext = await next().ConfigureAwait(false);

            await cacheActionFilterService.TryCacheValue(cacheKey, Expiration, variableKeys, executedContext).ConfigureAwait(false);
        }
    }
}