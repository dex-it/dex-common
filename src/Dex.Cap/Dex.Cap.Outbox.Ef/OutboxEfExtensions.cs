using System;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Outbox.Ef
{
    public static class OutboxEfExtensions
    {
        /// <summary>
        /// Регистрируем в контексте EF объект Outbox 
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OutboxModelCreating(this ModelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.Created);

            builder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.Status);

            builder.Entity<Models.OutboxEnvelope>()
                .HasIndex(o => o.Retries);
        }
    }
}