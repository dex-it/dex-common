using System;
using System.Threading.Tasks;
using Dex.DistributedCache.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.DistributedCache.ActionFilters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CacheActionFilter : ActionFilterAttribute
    {
        private readonly int _expiration;
        private readonly bool _isUserVariableKey;

        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// CacheActionFilter
        /// </summary>
        /// <param name="expiration">Absolute expiration time in seconds, relative to now</param>
        /// <param name="isUserVariableKey">Indicates, that the key depends on the user</param>
        public CacheActionFilter(int expiration = 3600, bool isUserVariableKey = true)
        {
            _expiration = expiration;
            _isUserVariableKey = isUserVariableKey;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));

            _serviceProvider = context.HttpContext.RequestServices;
            var cacheService = _serviceProvider.GetRequiredService<ICacheService>();

            var userId = GetUserId();
            var cacheKey = cacheService.GenerateCacheKey(userId, context);

            if (await cacheService.CheckExistingCacheValue(cacheKey, context).ConfigureAwait(false)) return;

            var executedContext = await next().ConfigureAwait(false);

            await cacheService.TryCacheValue(cacheKey, _expiration, userId, executedContext).ConfigureAwait(false);
        }

        private Guid GetUserId()
        {
            var userId = Guid.Empty;
            if (!_isUserVariableKey) return userId;

            var userIdService = _serviceProvider!.GetRequiredService<ICacheUserVariableService>();
            userId = userIdService.UserId;

            return userId;
        }
    }
}