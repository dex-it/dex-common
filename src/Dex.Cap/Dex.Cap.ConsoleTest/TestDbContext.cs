using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.ConsoleTest
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.OutboxModelCreating();
        }
    }
}