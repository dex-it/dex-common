using AuditGrpcServer;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Implementations.Grpc.Options;
using Dex.Audit.Client.Implementations.Grpc.Services;
using Dex.Audit.Client.Implementations.Grpc.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Implementations.Grpc.Extensions;

/// <summary>
/// Static class containing extension methods for configuring dependencies with added Grpc support.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the necessary audit dependencies with added Grpc support.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="configuration"><see cref="IConfiguration"/></param>
    /// <param name="configureClient">Configuration of <see cref="HttpClient"/> that will use the grpc client</param>
    /// <typeparam name="TAuditEventConfigurator"></typeparam>
    /// <typeparam name="TAuditCacheRepository"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGrpcAuditClient<TAuditEventConfigurator, TAuditCacheRepository>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<HttpMessageHandler>? configureClient = null)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditCacheRepository : class, IAuditSettingsCacheRepository
    {
        services.Configure<AuditGrpcOptions>(opts =>
            configuration.GetSection(nameof(AuditGrpcOptions)).Bind(opts));
        var grpcClientBuilder = services
            .AddGrpcClient<AuditSettingsService.AuditSettingsServiceClient>((provider, factoryOptions) =>
            {
                var options = provider.GetRequiredService<IOptions<AuditGrpcOptions>>().Value;
                factoryOptions.Address = new Uri(options.ServerAddress);
            });

        if (configureClient != null)
        {
            grpcClientBuilder
                .ConfigurePrimaryHttpMessageHandler(configureClient);
        }

        services.AddHostedService<GrpcAuditBackgroundWorker>();
        services.AddAuditClient<TAuditEventConfigurator, TAuditCacheRepository, GrpcAuditSettingsService>(configuration);

        return services;
    }
}
