using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Extensions;
using Dex.Audit.Server.Grpc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Grpc.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей с добавленным Grpc.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости.
    /// </summary>
    public static IServiceCollection AddGrpcAuditServer<TAuditRepository, TAuditSettingsRepository>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditRepository : class, IAuditPersistentRepository
        where TAuditSettingsRepository : class, IAuditCacheRepository
    {
        services.AddGrpc();
        services.AddSingleton<GrpcAuditServerSettingsService>();
        services.AddAuditServer<TAuditRepository, TAuditSettingsRepository,
            AuditSettingsServiceWithGrpcNotifier>(configuration);

        return services;
    }
}
