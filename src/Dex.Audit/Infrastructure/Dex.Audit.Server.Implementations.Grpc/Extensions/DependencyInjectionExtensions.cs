using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Extensions;
using Dex.Audit.Server.Grpc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Grpc.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the dependencies necessary for the audit with grpc to work.
    /// </summary>
    public static IServiceCollection AddGrpcAuditServer<TAuditEventsRepository, TAuditSettingsRepository, TAuditSettingsCacheRepository>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventsRepository : class, IAuditEventsRepository
        where TAuditSettingsRepository : class, IAuditSettingsRepository
        where TAuditSettingsCacheRepository : class, IAuditSettingsCacheRepository
    {
        services.AddGrpc();
        services.AddSingleton<GrpcAuditServerSettingsService>();
        services.AddAuditServer<TAuditEventsRepository, TAuditSettingsRepository, TAuditSettingsCacheRepository, AuditSettingsServiceWithGrpcNotifier>(configuration);

        return services;
    }
}
