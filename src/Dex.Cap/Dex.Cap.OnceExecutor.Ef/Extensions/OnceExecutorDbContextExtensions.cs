﻿using System;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef.Extensions
{
    public static class OnceExecutorDbContextExtensions
    {
        /// <summary>
        /// Регистрируем в контексте EF объект LastTransaction 
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OnceExecutorModelCreating(this ModelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var eb = builder.Entity<LastTransaction>();
            eb.HasKey(l => l.IdempotentKey);
            eb.HasIndex(l => l.Created);
        }
    }
}