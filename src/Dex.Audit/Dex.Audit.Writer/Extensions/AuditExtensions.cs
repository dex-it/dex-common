using Dex.Audit.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Writer.Extensions;

public static class AuditExtensions
{
    /// <summary>
    /// Добавляет контекст.
    /// </summary>
    public static IServiceCollection AddAuditDbContext<T>(this IServiceCollection services,  Action<DbContextOptionsBuilder>? optionsAction)
        where T : DbContext, IAuditContext
    {
        return services
            .AddDbContext<T>(optionsAction)
            .AddScoped<IAuditContext>(provider => provider.GetRequiredService<T>());
    }
}