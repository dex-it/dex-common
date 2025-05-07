using Dex.Audit.Implementations.Common.Repositories;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Extensions;
using Dex.Audit.Server.Implementations.Repositories;
using Dex.Audit.Server.Implementations.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Implementations.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add the dependencies necessary for the audit to work with default implementations:
    /// <see cref="SimpleAuditEventsRepository{TDbContext}"/>, <see cref="SimpleAuditSettingsRepository{TDbContext}"/>,
    /// <see cref="SimpleAuditSettingsCacheRepository"/>.
    /// </summary>
    public static IServiceCollection AddSimpleAuditServer<TDbContext, TAuditServerSettingsServer>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditServerSettingsServer : class, IAuditServerSettingsService
        where TDbContext : DbContext
    {
        services.AddAuditServer<SimpleAuditEventsRepository<TDbContext>, SimpleAuditSettingsRepository<TDbContext>,
            SimpleAuditSettingsCacheRepository, TAuditServerSettingsServer>(configuration);

        return services;
    }

    /// <summary>
    /// Add the dependencies necessary for the audit to work with default implementations:
    /// <see cref="SimpleAuditEventsRepository{TDbContext}"/>, <see cref="SimpleAuditSettingsRepository{TDbContext}"/>,
    /// <see cref="SimpleAuditSettingsCacheRepository"/>, <see cref="SimpleAuditServerSettingsService"/>.
    /// </summary>
    public static IServiceCollection AddSimpleAuditServer<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services
            .AddAuditServer<SimpleAuditEventsRepository<TDbContext>, SimpleAuditSettingsRepository<TDbContext>,
                SimpleAuditSettingsCacheRepository, SimpleAuditServerSettingsService>(configuration);

        return services;
    }
}
