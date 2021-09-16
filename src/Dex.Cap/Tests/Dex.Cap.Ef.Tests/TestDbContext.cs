using System;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Ef.Tests
{
    public class TestDbContext : DbContext
    {
        private readonly string _dbName;

        private static readonly ILoggerFactory _loggerFactory
           = LoggerFactory.Create(builder =>
           {
               builder.AddDebug();
           });

        public DbSet<User> Users { get; set; }

        public TestDbContext(string dbName)
        {
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql($"Server=127.0.0.1;Port=5432;Database={_dbName};User Id=postgres;Password=my-pass~003;",
                builder => { builder.EnableRetryOnFailure(); });

            optionsBuilder.UseLoggerFactory(_loggerFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var userEntity = modelBuilder.Entity<User>();
            userEntity.HasKey(x => x.Id);
            userEntity.HasIndex(x => x.Name).IsUnique();

            modelBuilder.OnceExecutorModelCreating();
            modelBuilder.OutboxModelCreating();
        }
    }
}