using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.SecurityTokenProvider.Extentions
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config">
        ///     Must contains section with name "TokenProviderOptions". "ApplicationName" and "ApiResource" is
        ///     required
        /// </param>
        /// <typeparam name="TokenStorage">implementation if "ITokenInfoStorage"</typeparam>
        public static IServiceCollection AddSecurityTokenProvider<TokenStorage>(this IServiceCollection services, IConfigurationSection config)
            where TokenStorage : class, ITokenInfoStorage
        {
            services.AddOptions<TokenProviderOptions>()
                .Bind(config)
                .ValidateDataAnnotations();


            services.AddSingleton<ITokenInfoStorage, TokenStorage>();  
            services.AddSingleton<ITokenProvider, TokenProvider>();
            return services;
        }
    }
}