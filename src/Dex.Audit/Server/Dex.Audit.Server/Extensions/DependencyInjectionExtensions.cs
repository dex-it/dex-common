using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости.
    /// </summary>
    public static IServiceCollection AddAuditServer<TAuditRepository, TAuditSettingsRepository, TAuditServerSettingsServer>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditRepository : class, IAuditPersistentRepository
        where TAuditSettingsRepository : class, IAuditCacheRepository
        where TAuditServerSettingsServer : class, IAuditServerSettingsService 
    {
        services
            .AddScoped(typeof(IAuditPersistentRepository), typeof(TAuditRepository))
            .AddScoped(typeof(IAuditCacheRepository), typeof(TAuditSettingsRepository))
            .AddScoped(typeof(IAuditServerSettingsService), typeof(TAuditServerSettingsServer))
            .Configure<AuditCacheOptions>(opts =>
                configuration.GetSection(nameof(AuditCacheOptions)).Bind(opts));

        return services;
    }
}
