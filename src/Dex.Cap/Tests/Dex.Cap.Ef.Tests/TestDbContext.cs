using System;
using System.IO;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Ef.Tests
{
    public class TestDbContext : DbContext
    {
        public static bool IsRetryStrategy { get; set; } = true;

        private static readonly ILoggerFactory LogFactory =
            LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.AddProvider(new TestLoggerProvider());
            });

        private readonly string _dbName;

        public DbSet<TestUser> Users => Set<TestUser>();

        public TestDbContext(string dbName)
        {
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true);
            var config = builder.Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            connectionString = connectionString.Replace("_dbName_", _dbName);
            optionsBuilder.UseNpgsql(connectionString,
                options =>
                {
                    if (IsRetryStrategy)
                    {
                        options.EnableRetryOnFailure();    
                    }
                });

            optionsBuilder.UseLoggerFactory(LogFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var userEntity = modelBuilder.Entity<TestUser>();
            userEntity.HasKey(x => x.Id);
            userEntity.HasIndex(x => x.Name).IsUnique();

            modelBuilder.OnceExecutorModelCreating();
            modelBuilder.OutboxModelCreating();
        }

        // ReSharper disable once UnusedType.Local
        private sealed class DateTimeKindValueConverter : ValueConverter<DateTime, DateTime>
        {
            public DateTimeKindValueConverter(DateTimeKind kind, ConverterMappingHints mappingHints = null!)
                : base(dateTime => dateTime.ToUniversalTime(), // Что-бы в timestamp дата всегда хранилась в UTC.
                    dateTime => DateTime.SpecifyKind(dateTime, kind)
                        .ToLocalTime(), // timestamp в базе эквивалентен `Unspecified DateTime` поэтому просто восстанавливаем заведомо известный Kind.
                    mappingHints)
            {
            }
        }
    }
}