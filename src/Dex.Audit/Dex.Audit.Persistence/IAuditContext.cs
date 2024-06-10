using Dex.Audit.Domain.Models;
using Dex.Audit.Domain.Models.AuditEvent;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Persistence;

/// <summary>
/// Интерфейс контекста базы аудита.
/// </summary>
public interface IAuditContext
{
    /// <summary>
    /// События аудита.
    /// </summary>
    DbSet<AuditEvent> AuditEvents { get; set; }

    /// <summary>
    /// Настройки событий.
    /// </summary>
    DbSet<AuditSettings> AuditSettings { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}