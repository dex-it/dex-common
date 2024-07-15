using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Options;
using Dex.Audit.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuditWriter = Dex.Audit.Client.Services.AuditWriter;

namespace Dex.Audit.Client.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет необходимые для работы аудита зависимости
    /// </summary>
    public static IServiceCollection AddAuditClient<TAuditEventConfigurator, TAuditSettingsRepository>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TAuditEventConfigurator : class, IAuditEventConfigurator
        where TAuditSettingsRepository : class, IAuditSettingsRepository
    {
        services
            .AddScoped<IAuditOutputProvider, AuditOutputProvider>()
            .AddScoped<IAuditWriter, AuditWriter>()
            .AddScoped(typeof(IAuditEventConfigurator), typeof(TAuditEventConfigurator))
            .AddScoped(typeof(IAuditSettingsRepository), typeof(TAuditSettingsRepository))
            .Configure<AuditEventOptions>(opts => configuration.GetSection(nameof(AuditEventOptions)).Bind(opts));

        return services;
    }
}
