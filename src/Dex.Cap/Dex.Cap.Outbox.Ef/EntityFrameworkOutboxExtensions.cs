using System;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Outbox.Ef
{
    public static class EntityFrameworkOutboxExtensions
    {
        /// <summary>
        /// Регистрируем в контексте EF объект Outbox 
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OutboxModelCreating(this ModelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Entity<Models.Outbox>()
                .Property(o => o.CorrelationId)
                .HasDefaultValueSql("uuid_generate_v4()");

            builder.Entity<Models.Outbox>()
                .HasIndex(o => o.Created);

            builder.Entity<Models.Outbox>()
                .HasIndex(o => o.Status);

            builder.Entity<Models.Outbox>()
                .HasIndex(o => o.OutboxMessageType);

            builder.Entity<Models.Outbox>()
                .HasIndex(o => o.Retries);
        }
    }
}