using Dex.Audit.Domain.Entities;
using Dex.Audit.EF.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.EF.Extensions;

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