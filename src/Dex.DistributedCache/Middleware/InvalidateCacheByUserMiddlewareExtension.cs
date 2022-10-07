using Microsoft.AspNetCore.Builder;

namespace Dex.DistributedCache.Middleware
{
    public static class InvalidateCacheByUserMiddlewareExtension
    {
        public static void UseInvalidateCacheByUserMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<InvalidateCacheByUserMiddleware>();
        }
    }
}