using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add the dependencies necessary for the audit to work.
    /// </summary>
    public static IServiceCollection AddAuditServer
        <TAuditEventsRepository, TAuditSettingsRepository, TAuditCacheRepository, TAuditServerSettingsServer>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventsRepository : class, IAuditEventsRepository
        where TAuditSettingsRepository : class, IAuditSettingsRepository
        where TAuditCacheRepository : class, IAuditSettingsCacheRepository
        where TAuditServerSettingsServer : class, IAuditServerSettingsService
    {
        services
            .AddScoped(typeof(IAuditEventsRepository), typeof(TAuditEventsRepository))
            .AddScoped(typeof(IAuditSettingsRepository), typeof(TAuditSettingsRepository))
            .AddScoped(typeof(IAuditSettingsCacheRepository), typeof(TAuditCacheRepository))
            .AddScoped(typeof(IAuditServerSettingsService), typeof(TAuditServerSettingsServer))
            .Configure<AuditCacheOptions>(opts =>
                configuration.GetSection(nameof(AuditCacheOptions)).Bind(opts));

        return services;
    }
}
