using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dex.SecurityTokenProvider.Extentions
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="config">Must contains section with name "TokenProviderOptions". "ApplicationName" and "ApiResource" is required </param>
        ///
        /// <typeparam name="TokenStorage">implementation if "ITokenInfoStorage"</typeparam>
        public static IServiceCollection AddDexSecurityTokenProvider<TokenStorage>(this IServiceCollection services, IConfigurationSection config)
            where TokenStorage : class, ITokenInfoStorage
        {
            AddOptions(services, config);

            services.AddScoped<ITokenInfoStorage, TokenStorage>();
            services.AddScoped<ITokenProvider, TokenProvider>();
            return services;
        }


        private static void AddOptions(IServiceCollection services, IConfigurationSection config)
        {
            services.AddOptions<TokenProviderOptions>()
                .Bind(config)
                .ValidateDataAnnotations();

            //пробуем получить инстанс при  старте,для того чтобы  сработала валидация
            var unused = services.BuildServiceProvider().GetRequiredService<IOptions<TokenProviderOptions>>().Value;
        }
    }
}