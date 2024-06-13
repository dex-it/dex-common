using Dex.Audit.Contracts.Interfaces;
using Dex.Audit.Contracts.Options;
using Dex.Audit.Publisher.Services;
using Dex.Audit.Publisher.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Publisher.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости
    /// </summary>
    public static IServiceCollection AddAudit<TAuditEventConfigurator>(this IServiceCollection services, IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
    {
        services
            .AddHostedService<SubsystemAuditWorker>()
            .AddScoped<IAuditPublisher, AuditPublisherRabbit>()
            .AddScoped<IAuditManager, AuditManager>()
            .AddScoped(typeof(IAuditEventConfigurator), typeof(TAuditEventConfigurator))
            .Configure<AuditEventOptions>(opts => configuration.GetSection(nameof(AuditEventOptions)).Bind(opts));

        return services;
    }
}
