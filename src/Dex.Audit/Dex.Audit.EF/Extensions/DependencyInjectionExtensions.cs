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
    public static IServiceCollection AddAuditInterceptors(this IServiceCollection services)
    {
        services
            .AddScoped<IInterceptionAndSendingEntriesService, InterceptionAndSendingEntriesService>()
            .AddScoped<IDbTransactionInterceptor, AuditTransactionInterceptor>()
            .AddScoped<ISaveChangesInterceptor, AuditSaveChangesInterceptor>();

        return services;
    }
}
