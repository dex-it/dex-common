using System;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public static class OnceExecutorEfExtensions
    {
        /// <summary>
        /// Регистрируем в контексте EF объект LastTransaction 
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OnceExecutorModelCreating(this ModelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Entity<LastTransaction>()
                .HasIndex(l => l.Created);
        }
    }
}