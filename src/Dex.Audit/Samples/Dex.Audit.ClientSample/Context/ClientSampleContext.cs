using Dex.Audit.ClientSample.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ClientSample.Context;

public class ClientSampleContext(DbContextOptions<ClientSampleContext> dbContextOptions) : DbContext(dbContextOptions)
{
    public DbSet<User> Users { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuditDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(s => s.Id);
    }
}