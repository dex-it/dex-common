using System;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;
using Dex.DistributedCache.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1031

namespace Dex.DistributedCache.Middleware
{
    public class InvalidateCacheByUserMiddleware
    {
        private const string InvalidateHeader = "ForceInvalidateCacheByUser";

        private readonly RequestDelegate _next;

        public InvalidateCacheByUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var serviceProvider = context.RequestServices;
            var logger = serviceProvider.GetRequiredService<ILogger<InvalidateCacheByUserMiddleware>>();

            try
            {
                var cacheService = serviceProvider.GetRequiredService<ICacheService>();
                var userIdService = serviceProvider.GetRequiredService<ICacheUserVariableKeyResolver>();

                if (context.Request.Headers.ContainsKey(InvalidateHeader))
                {
                    var dependencies = new[] { new CacheDependency(userIdService.GetVariableKey()) };
                    await cacheService.InvalidateByDependenciesAsync(dependencies, context.RequestAborted).ConfigureAwait(false);
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