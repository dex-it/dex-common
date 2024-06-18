using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Options;
using Dex.Audit.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Client.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости
    /// </summary>
    public static IServiceCollection AddAudit<TAuditEventConfigurator, TAuditRepository, TAuditSettingsRepository>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditRepository : class, IAuditRepository
        where TAuditSettingsRepository : class, IAuditSettingsRepository
    {
        services
            .AddScoped<IAuditPublisher, AuditPublisherRabbit>()
            .AddScoped<IAuditManager, AuditManager>()
            .AddScoped(typeof(IAuditEventConfigurator), typeof(TAuditEventConfigurator))
            .AddScoped(typeof(IAuditRepository), typeof(TAuditRepository))
            .AddScoped(typeof(IAuditSettingsRepository), typeof(TAuditSettingsRepository))
            .Configure<AuditEventOptions>(opts => configuration.GetSection(nameof(AuditEventOptions)).Bind(opts));

        return services;
    }
}
