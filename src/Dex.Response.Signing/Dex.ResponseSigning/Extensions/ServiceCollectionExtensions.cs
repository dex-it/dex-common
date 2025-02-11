using System.Text.Json;
using Dex.ResponseSigning.Handlers;
using Dex.ResponseSigning.Jws;
using Dex.ResponseSigning.KeyExtractor;
using Dex.ResponseSigning.Options;
using Dex.ResponseSigning.Serialization;
using Dex.ResponseSigning.Signing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dex.ResponseSigning.Extensions;

/// <summary>
/// Методы расширений для добавления необходимых зависимостей в DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляем зависимости для подписи ответа сервиса
    /// </summary>
    public static IServiceCollection AddResponseSigning(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton<IPrivateKeyExtractor, DefaultPrivateKeyExtractor>();

        services
            .TryAddSingleton(new SigningDataSerializationOptions(new JsonSerializerOptions()));

        services
            .AddTransient<InternalDefaultCertificateExtractor>()
            .AddTransient<ISignDataService, SignDataService>()
            .AddTransient<IJwsSignatureService, JwsSignatureService>();

        services.Configure<ResponseSigningOptions>(configuration.GetSection("ResponseSigningOptions"));

        return services;
    }

    /// <summary>
    /// Добавляем зависимости для верификации ответа сервиса
    /// </summary>
    public static IServiceCollection AddResponseVerifying(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddSingleton<IPublicKeyExtractor, DefaultPublicKeyExtractor>();

        services
            .TryAddSingleton(new SigningDataSerializationOptions(new JsonSerializerOptions()));

        services
            .AddTransient<InternalDefaultCertificateExtractor>()
            .AddTransient<IVerifySignService, VerifySignService>()
            .AddTransient<IJwsParsingService, JwsParsingService>()
            .AddTransient<SignatureVerificationHandler>();

        services.Configure<ResponseSigningOptions>(configuration.GetSection("ResponseSigningOptions"));

        return services;
    }

    public static IServiceCollection AddSigningDataSerializationOptions(this IServiceCollection services,
        JsonSerializerOptions options)
    {
        return services.AddSingleton(new SigningDataSerializationOptions(options));
    }
}