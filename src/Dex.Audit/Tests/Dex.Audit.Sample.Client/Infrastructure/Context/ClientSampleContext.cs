using Dex.Audit.Sample.Client.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Sample.Client.Infrastructure.Context;

public class ClientSampleContext(DbContextOptions<ClientSampleContext> dbContextOptions) : DbContext(dbContextOptions)
{
    public DbSet<User> Users { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuditDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(s => s.Id);
    }
}