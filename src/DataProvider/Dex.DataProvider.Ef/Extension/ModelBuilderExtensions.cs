using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dex.DataProvider.Ef.Extension
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder UseValueConverterForType<TModel, TProvider>(
            this ModelBuilder modelBuilder,
            ValueConverter<TModel, TProvider> converter)
        {
            return UseValueConverterForType(modelBuilder, typeof(TModel), converter);
        }

        private static ModelBuilder UseValueConverterForType(
            this ModelBuilder modelBuilder, 
            Type type,
            ValueConverter converter)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(p => p.PropertyType == type);
                
                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(converter);
                }
            }

            return modelBuilder;
        }
    }
}