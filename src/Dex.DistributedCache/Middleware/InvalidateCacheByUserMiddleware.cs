using System;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;
using Dex.DistributedCache.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.DistributedCache.Middleware
{
    public class InvalidateCacheByUserMiddleware
    {
        private const string InvalidateHeader = "InvalidateCacheByUser";

        private readonly RequestDelegate _next;
        private IServiceProvider? _serviceProvider;
        private ILogger<InvalidateCacheByUserMiddleware>? _logger;

        public InvalidateCacheByUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                _serviceProvider = context.RequestServices;
                _logger = _serviceProvider.GetRequiredService<ILogger<InvalidateCacheByUserMiddleware>>();
                var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
                var userIdService = _serviceProvider.GetRequiredService<ICacheUserVariableService>();

                _logger.LogDebug("Run InvalidateCacheByUserMiddleware");

                if (context.Request.Headers.ContainsKey(InvalidateHeader))
                {
                    var values = new[] { userIdService.UserId.ToString() };
                    await cacheService.InvalidateByDependenciesAsync(new[] { new CachePartitionedDependencies("user", values) }, context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}