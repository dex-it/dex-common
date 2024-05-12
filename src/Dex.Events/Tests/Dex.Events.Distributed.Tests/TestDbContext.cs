using System;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Events.Distributed.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Events.Distributed.Tests
{
    public class TestDbContext : DbContext
    {
        private static readonly ILoggerFactory LogFactory = LoggerFactory.Create(builder => { builder.AddDebug(); });
        private readonly string _dbName;

        public DbSet<User> Users { get; internal set; }

        public TestDbContext(string dbName)
        {
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseNpgsql($"Server=127.0.0.1;Port=5432;Database={_dbName};User Id=postgres;Password=my-pass~003;",
                builder => { builder.EnableRetryOnFailure(); });

            optionsBuilder.UseLoggerFactory(LogFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var userEntity = modelBuilder.Entity<User>();
            userEntity.HasKey(x => x.Id);
            userEntity.HasIndex(x => x.Name).IsUnique();

            modelBuilder.OutboxModelCreating();
        }
    }
}