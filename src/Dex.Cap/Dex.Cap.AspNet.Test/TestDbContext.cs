using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox.Ef.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.AspNet.Test;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.OutboxModelCreating();
        modelBuilder.OnceExecutorModelCreating();
    }
}