using System;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Outbox.Ef
{
    public static class OutboxEfExtensions
    {
        /// <summary>
        /// Регистрируем в контексте EF объект Outbox.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OutboxModelCreating(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.CreatedUtc);

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.Status)
                .HasFilter("\"Status\" in (0,1)");

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.Retries);

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.CorrelationId);

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.CreatedUtc)
                .HasConversion(t => t.ToUniversalTime(), f => DateTime.SpecifyKind(f, DateTimeKind.Utc).ToLocalTime());

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.LockExpirationTimeUtc)
                .HasConversion<DateTime?>(
                    t => t == null ? null : t.Value.ToUniversalTime(),
                    f => f == null ? null : DateTime.SpecifyKind(f.Value, DateTimeKind.Utc).ToLocalTime());

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.Updated)
                .HasConversion<DateTime?>(
                    t => t == null ? null : t.Value.ToUniversalTime(),
                    f => f == null ? null : DateTime.SpecifyKind(f.Value, DateTimeKind.Utc).ToLocalTime());

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.LockTimeout)
                .HasDefaultValue(TimeSpan.FromSeconds(30))
                .HasComment("Maximum allowable blocking time");

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.LockId)
                .HasComment("Idempotency key (unique key of the thread that captured the lock)");

            modelBuilder.Entity<Models.OutboxEnvelope>()
                .Property(x => x.LockExpirationTimeUtc)
                .HasComment("Preventive timeout (maximum lifetime of actuality 'LockId')");
        }
    }
}