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
            if (modelStore == null) throw new ArgumentNullException(nameof(modelStore));
            
            ModeTypes = modelStore.GetModels();
        }

        protected ModelDbContext(IModelStore modelStore, DbContextOptions options)
            : base(options)
        {
            if (modelStore == null) throw new ArgumentNullException(nameof(modelStore));
            
            ModeTypes = modelStore.GetModels();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            
            base.OnModelCreating(modelBuilder);

            foreach (var modeType in ModeTypes)
            {
                modelBuilder.Entity(modeType);
            }

            UseDateTimeConverter(modelBuilder);
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