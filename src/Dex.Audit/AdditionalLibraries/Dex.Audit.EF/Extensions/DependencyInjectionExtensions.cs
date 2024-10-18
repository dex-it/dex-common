using Dex.Audit.EF.Interceptors;
using Dex.Audit.EF.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.EF.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add an AuditInterceptor.
    /// </summary>
    public static IServiceCollection AddAuditInterceptors<TInterceptionAndSendingEntriesService>(
        this IServiceCollection services)
        where TInterceptionAndSendingEntriesService : class, IInterceptionAndSendingEntriesService
    {
        services
            .AddScoped(typeof(IInterceptionAndSendingEntriesService), typeof(TInterceptionAndSendingEntriesService))
            .AddScoped<IAuditDbTransactionInterceptor, AuditTransactionInterceptor>()
            .AddScoped<IAuditSaveChangesInterceptor, AuditSaveChangesInterceptor>();

        return services;
    }
}
