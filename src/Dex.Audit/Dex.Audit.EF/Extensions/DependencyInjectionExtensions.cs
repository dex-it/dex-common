using Dex.Audit.EF.Interceptors;
using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.EF.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавляет AuditInterceptor
    /// </summary>
    public static IServiceCollection AddAuditInterceptors<TInterceptionAndSendingEntriesService>(this IServiceCollection services)
        where TInterceptionAndSendingEntriesService : IInterceptionAndSendingEntriesService
    {
        services
            .AddScoped(typeof(IInterceptionAndSendingEntriesService), typeof(TInterceptionAndSendingEntriesService))
            .AddScoped<IAuditDbTransactionInterceptor, AuditTransactionInterceptor>()
            .AddScoped<IAuditSaveChangesInterceptor, AuditSaveChangesInterceptor>();

        return services;
    }
}
