using Dex.Audit.Domain.Entities;
using Dex.Audit.EF.NpgSql.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Context;

public class AuditServerDbContext : DbContext
{
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<AuditSettings> AuditSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuditDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());

        modelBuilder.Entity<AuditEvent>().HasKey(e => e.ExternalId);
        modelBuilder.Entity<AuditSettings>().HasKey(s => s.Id);
    }
}