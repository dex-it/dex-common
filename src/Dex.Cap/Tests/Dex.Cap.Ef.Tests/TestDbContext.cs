using System;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Ef.Tests
{
    public class TestDbContext : DbContext
    {
        private static readonly ILoggerFactory LogFactory =
            LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.AddProvider(new TestLoggerProvider());
            });

        private readonly string _dbName;

        public DbSet<User> Users => Set<User>();

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

            modelBuilder.OnceExecutorModelCreating();
            modelBuilder.OutboxModelCreating();
        }

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