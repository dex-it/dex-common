using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.AspNet.Test
{
    public class TestDbContext : DbContext
    {
        private static readonly ILoggerFactory _loggerFactory
            = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
            });

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseLoggerFactory(_loggerFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.OutboxModelCreating();
        }
    }
}