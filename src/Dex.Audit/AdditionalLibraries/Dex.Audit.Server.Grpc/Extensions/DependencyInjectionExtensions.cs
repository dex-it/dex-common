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
