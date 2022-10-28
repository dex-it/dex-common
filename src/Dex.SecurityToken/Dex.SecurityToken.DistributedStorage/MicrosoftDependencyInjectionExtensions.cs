using Dex.SecurityTokenProvider.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.SecurityToken.DistributedStorage
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        /// <summary>
        /// Register DistributedTokenStorageProvider, need call after services.AddSecurityTokenProvider()
        /// Based on IDistributedCache
        /// </summary>
        /// <param name="services">Service collection</param>
        public static IServiceCollection AddDistributedTokenInfoStorage(this IServiceCollection services)
        {
            services.AddScoped<IDistributedCacheTypedClient, DistributedCacheTypedClient>();
            services.AddScoped<ITokenInfoStorage, DistributedTokenStorageProvider>();

            return services;
        }
    }
}