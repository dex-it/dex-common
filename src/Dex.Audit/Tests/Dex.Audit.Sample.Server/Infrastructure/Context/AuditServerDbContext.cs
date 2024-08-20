using Dex.Audit.Domain.Entities;
using Dex.Audit.EF.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Infrastructure.Context;

public class AuditServerDbContext : DbContext
{
    public DbSet<AuditEvent> AuditEvents { get; init; }
    public DbSet<AuditSettings> AuditSettings { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuditDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());

        modelBuilder.Entity<AuditSettings>().HasKey(s => s.Id);
    }
}