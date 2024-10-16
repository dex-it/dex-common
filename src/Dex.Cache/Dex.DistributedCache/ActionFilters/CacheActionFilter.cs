using System;
using System.Threading.Tasks;
using Dex.DistributedCache.Exceptions;
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
        public Type[] CacheVariableKeyResolvers { get; }

        /// <summary>
        /// CacheActionFilter
        /// </summary>
        /// <param name="expiration">Absolute expiration time in seconds, relative to now</param>
        /// <param name="cacheVariableKeyResolvers">Variable key interfaces</param>
        public CacheActionFilter(int expiration = 3600, params Type[] cacheVariableKeyResolvers)
        {
            Expiration = expiration;
            CacheVariableKeyResolvers = cacheVariableKeyResolvers;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            var serviceProvider = context.HttpContext.RequestServices;
            var cacheService = serviceProvider.GetRequiredService<ICacheManagementService>();
            var cacheActionFilterService = serviceProvider.GetRequiredService<ICacheActionFilterService>();

            var variableKeys = cacheActionFilterService.GetVariableKeys(CacheVariableKeyResolvers);
            var paramsList = new[] { CacheHelper.GetDisplayUrl(context.HttpContext.Request) };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            if (await cacheActionFilterService.CheckExistingCacheValue(cacheKey, context).ConfigureAwait(false))
                return;

            var executedContext = await next().ConfigureAwait(false);

            if (executedContext.Exception != null)
            {
                throw new CacheActionFilterException("ActionExecutedContext Exception", executedContext.Exception);
            }

            await cacheActionFilterService.TryCacheValue(cacheKey, Expiration, executedContext).ConfigureAwait(false);
        }
    }
}