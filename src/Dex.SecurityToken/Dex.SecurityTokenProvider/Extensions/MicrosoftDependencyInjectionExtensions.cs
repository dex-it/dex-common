using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.SecurityTokenProvider.Extensions
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
        /// <typeparam name="TTokenStorage">implementation if "ITokenInfoStorage"</typeparam>
        public static IServiceCollection AddSecurityTokenProvider<TTokenStorage>(this IServiceCollection services, IConfigurationSection config)
            where TTokenStorage : class, ITokenInfoStorage
        {
            services.AddOptions<TokenProviderOptions>()
                .Bind(config)
                .ValidateDataAnnotations();


            services.AddSingleton<IDataProtectionFactory, DataProtectionFactory>();
            
            services.AddScoped<ITokenInfoStorage, TTokenStorage>();  
            services.AddScoped<ITokenProvider, TokenProvider>();
            return services;
        }
    }
}