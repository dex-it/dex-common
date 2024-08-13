using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
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
    public static IServiceCollection AddAuditServer<TAuditRepository, TAuditSettingsRepository>(
        this IServiceCollection services)
        where TAuditRepository : class, IAuditPersistentRepository
        where TAuditSettingsRepository : class, IAuditCacheRepository
    {
        services
            .AddScoped(typeof(IAuditPersistentRepository), typeof(TAuditRepository))
            .AddScoped(typeof(IAuditCacheRepository), typeof(TAuditSettingsRepository))
            .AddOptions<AuditCacheOptions>(nameof(AuditCacheOptions));

        return services;
    }
}
