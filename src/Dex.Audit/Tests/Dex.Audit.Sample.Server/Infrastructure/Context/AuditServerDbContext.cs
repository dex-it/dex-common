using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.Implementations.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Sample.Server.Infrastructure.Context;

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
        modelBuilder.AddAuditEntities();
    }
}