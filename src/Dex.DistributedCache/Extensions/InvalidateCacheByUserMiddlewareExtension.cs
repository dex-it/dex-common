using Dex.DistributedCache.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Dex.DistributedCache.Extensions
{
    public static class InvalidateCacheByUserMiddlewareExtension
    {
        public static void UseInvalidateCacheByUserMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<InvalidateCacheByUserMiddleware>();
        }
    }
}