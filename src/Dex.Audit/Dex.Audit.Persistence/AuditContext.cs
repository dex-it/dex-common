using System.Reflection;
using Dex.Audit.Domain.Models;
using Dex.Audit.Domain.Models.AuditEvent;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Persistence;

/// <summary>
/// Контекст базы аудита
/// </summary>
public class AuditContext : DbContext, IAuditContext
{
    public AuditContext(){}

    public AuditContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// События аудита
    /// </summary>
    public DbSet<AuditEvent> AuditEvents { get; set; }

    /// <summary>
    /// Настройки событий
    /// </summary>
    public DbSet<AuditSettings> AuditSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(typeof(AuditContext)) ?? throw new InvalidOperationException());

        modelBuilder.Entity<AuditEvent>().HasKey(e => e.ExternalId);
        modelBuilder.Entity<AuditSettings>().HasKey(s => s.Id);
    }
}
