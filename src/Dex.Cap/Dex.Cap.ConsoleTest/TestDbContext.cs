using System;
using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.ConsoleTest
{
    public class TestDbContext : DbContext
    {
        //public DbSet<User> Users { get; set; }


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

            //optionsBuilder.UseNpgsql(o => o.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), builder => { builder.EnableRetryOnFailure(); });

            optionsBuilder.UseLoggerFactory(_loggerFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //var userEntity = modelBuilder.Entity<User>();
            //userEntity.HasKey(x => x.Id);
            //userEntity.HasIndex(x => x.Name).IsUnique();

            //modelBuilder.OnceExecutorModelCreating();
            modelBuilder.OutboxModelCreating();
        }
    }
}