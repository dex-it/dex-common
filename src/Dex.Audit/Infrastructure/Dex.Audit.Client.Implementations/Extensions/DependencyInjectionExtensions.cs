using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Implementations.Services;
using Dex.Audit.Client.Services;
using Dex.Audit.Implementations.Common.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Client.Implementations.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the dependencies necessary for the audit client to work with default implementations:
    /// <see cref="SimpleAuditSettingsCacheRepository"/>.
    /// </summary>
    public static IServiceCollection AddSimpleAuditClient<TAuditEventConfigurator, TAuditSettingsService>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditSettingsService : class, IAuditSettingsService 
    {
        services
            .AddAuditClient<TAuditEventConfigurator, SimpleAuditSettingsCacheRepository, TAuditSettingsService>(configuration);

        return services;
    }

    /// <summary>
    /// Adds the dependencies necessary for the audit client to work with default implementations:
    /// <see cref="SimpleAuditSettingsCacheRepository"/>, <see cref="SimpleClientAuditSettingsService"/>.
    /// </summary>
    public static IServiceCollection AddSimpleAuditClient<TAuditEventConfigurator>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
    {
        services
            .AddAuditClient<TAuditEventConfigurator, SimpleAuditSettingsCacheRepository, SimpleClientAuditSettingsService>(configuration);

        return services;
    }

    /// <summary>
    /// Adds the dependencies necessary for the audit client to work with default implementations:
    /// <see cref="BaseAuditEventConfigurator"/>, <see cref="SimpleAuditSettingsCacheRepository"/>,
    /// <see cref="SimpleClientAuditSettingsService"/>.
    /// </summary>
    public static IServiceCollection AddSimpleAuditClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuditClient<SimpleAuditSettingsCacheRepository, SimpleClientAuditSettingsService>(configuration);

        return services;
    }
}