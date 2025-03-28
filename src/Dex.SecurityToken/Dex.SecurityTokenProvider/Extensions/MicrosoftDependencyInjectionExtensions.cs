using System;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.SecurityTokenProvider.Extensions
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        /// <summary>
        /// Add default configuration services for ITokenProvider
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config">
        ///     Must contains section with name "TokenProviderOptions". "ApplicationName" and "ApiResource" is
        ///     required
        /// </param>
        public static IServiceCollection AddSecurityTokenProvider(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddOptions<TokenProviderOptions>()
                .Bind(config)
                .ValidateDataAnnotations();

            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Add default configuration services for ITokenProvider
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure">Configuration action</param>
        public static IServiceCollection AddSecurityTokenProvider(this IServiceCollection services, Action<TokenProviderOptions> configure)
        {
            services.AddOptions<TokenProviderOptions>()
                .Configure(configure)
                .ValidateDataAnnotations();

            RegisterCoreServices(services);

            return services;
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.AddSingleton<IDataProtectionFactory, DataProtectionFactory>();
            services.AddScoped<ITokenInfoStorage, InMemoryTokenInfoStorage>();
            services.AddScoped<ITokenProvider, TokenProvider>();
        }
    }
}