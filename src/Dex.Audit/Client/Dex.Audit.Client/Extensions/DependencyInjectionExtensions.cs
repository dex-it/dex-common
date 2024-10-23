using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Options;
using Dex.Audit.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuditWriter = Dex.Audit.Client.Services.AuditWriter;

namespace Dex.Audit.Client.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the dependencies necessary for the audit client to work.
    /// </summary>
    public static IServiceCollection AddAuditClient<TAuditEventConfigurator, TAuditCacheRepository, TAuditSettingsService>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditCacheRepository : class, IAuditSettingsCacheRepository
        where TAuditSettingsService : class, IAuditSettingsService
    {
        services
            .AddScoped<IAuditOutputProvider, AuditOutputProvider>()
            .AddScoped<IAuditWriter, AuditWriter>()
            .AddScoped(typeof(IAuditEventConfigurator), typeof(TAuditEventConfigurator))
            .AddScoped(typeof(IAuditSettingsCacheRepository), typeof(TAuditCacheRepository))
            .AddScoped(typeof(IAuditSettingsService), typeof(TAuditSettingsService))
            .Configure<AuditEventOptions>(
                opts => configuration.GetSection(nameof(AuditEventOptions)).Bind(opts));

        return services;
    }

    /// <summary>
    /// Adds the dependencies necessary for the audit client to work with implementation of <see cref="BaseAuditEventConfigurator"/>.
    /// </summary>
    public static IServiceCollection AddAuditClient<TAuditCacheRepository, TAuditSettingsService>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditCacheRepository : class, IAuditSettingsCacheRepository
        where TAuditSettingsService : class, IAuditSettingsService
    {
        services
            .AddScoped<IAuditOutputProvider, AuditOutputProvider>()
            .AddScoped<IAuditWriter, AuditWriter>()
            .AddScoped(typeof(IAuditEventConfigurator), typeof(BaseAuditEventConfigurator))
            .AddScoped(typeof(IAuditSettingsCacheRepository), typeof(TAuditCacheRepository))
            .AddScoped(typeof(IAuditSettingsService), typeof(TAuditSettingsService))
            .Configure<AuditEventOptions>(
                opts => configuration.GetSection(nameof(AuditEventOptions)).Bind(opts));

        return services;
    }
}