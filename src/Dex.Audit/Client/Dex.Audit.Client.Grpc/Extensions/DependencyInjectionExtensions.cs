using AuditClient;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Grpc.Options;
using Dex.Audit.Client.Grpc.Services;
using Dex.Audit.Client.Grpc.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Grpc.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей с добавленным Grpc.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости с добавленным Grpc.
    /// </summary>
    public static IServiceCollection AddGrpcAuditClient<TAuditEventConfigurator, TAuditCacheRepository>(
        this IServiceCollection services)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditCacheRepository : class, IAuditCacheRepository
    {
        services.AddOptions<AuditGrpcOptions>(nameof(AuditGrpcOptions));
        services.AddGrpcClient<AuditSettingsService.AuditSettingsServiceClient>((provider, factoryOptions) =>
        {
            var options = provider.GetRequiredService<IOptions<AuditGrpcOptions>>().Value;
            factoryOptions.Address = new Uri(options.ServerAddress);
        });
        services.AddHostedService<GrpcAuditBackgroundWorker>();
        services.AddAuditClient<TAuditEventConfigurator, TAuditCacheRepository, GrpcAuditSettingsService>();

        return services;
    }
}
