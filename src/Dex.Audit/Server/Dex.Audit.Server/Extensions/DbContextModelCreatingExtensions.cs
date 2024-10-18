using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Server.Extensions;

public static class DbContextModelCreatingExtensions
{
    /// <summary>
    /// Add entities for audit work.
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void AddAuditEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());

        modelBuilder.Entity<AuditSettings>().HasKey(s => s.Id);
    }
}