using System;
using System.Threading.Tasks;
using Dex.DistributedCache.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1031

namespace Dex.DistributedCache.Middleware
{
    public class InvalidateCacheByUserMiddleware
    {
        private const string InvalidateHeader = "InvalidateCacheByUser";

        private readonly RequestDelegate _next;

        public InvalidateCacheByUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var serviceProvider = context.RequestServices;
            var logger = serviceProvider.GetRequiredService<ILogger<InvalidateCacheByUserMiddleware>>();

            try
            {
                var cacheService = serviceProvider.GetRequiredService<ICacheService>();
                var userIdService = serviceProvider.GetRequiredService<ICacheUserVariableKey>();

                logger.LogDebug("Run InvalidateCacheByUserMiddleware");

                if (context.Request.Headers.ContainsKey(InvalidateHeader))
                {
                    var values = new[] { userIdService.GetVariableKey() };
                    await cacheService.InvalidateByVariableKeyAsync<ICacheUserVariableKey>(values, context.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "{Header} error", InvalidateHeader);
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}