using Microsoft.Extensions.DependencyInjection;

namespace Dex.SecurityToken.RedisStorage
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddTokenRedisStorageServices(this IServiceCollection services)
        {
            services.AddScoped<IDistributedCacheTypedClient, DistributedCacheTypedClient>();

            return services;
        }
    }
}