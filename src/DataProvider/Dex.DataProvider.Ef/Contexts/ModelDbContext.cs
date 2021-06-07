using System;
using System.Collections.Generic;
using Dex.DataProvider.Ef.Contracts;
using Dex.DataProvider.Ef.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dex.DataProvider.Ef.Contexts
{
    public abstract class ModelDbContext : DbContext
    {
        protected IEnumerable<Type> ModeTypes { get; }

        protected ModelDbContext(IModelStore modelStore)
        {
            ModeTypes = modelStore.GetModels();
        }

        protected ModelDbContext(IModelStore modelStore, DbContextOptions options)
            : base(options)
        {
            ModeTypes = modelStore.GetModels();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var modeType in ModeTypes)
            {
                builder.Entity(modeType);
            }

            UseDateTimeConverter(builder);
        }
        
        private static void UseDateTimeConverter(ModelBuilder modelBuilder)
        {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            modelBuilder.UseValueConverterForType(dateTimeConverter);
            
            var dateTimeNullableConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
            modelBuilder.UseValueConverterForType(dateTimeNullableConverter);
        }
    }
}