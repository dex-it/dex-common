using System;
using System.Linq;
using Dex.DataProvider.Contracts.Entities;
using Dex.DataProvider.Ef.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dex.DataProvider.Ef.Extension
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder HasIndexCreatedUtc(
            this ModelBuilder modelBuilder,
            IModelStore modelStore)
        {
            return HasIndex(modelBuilder, modelStore, typeof(ICreatedUtc), nameof(ICreatedUtc.CreatedUtc));
        }
        
        public static ModelBuilder HasIndexUpdatedUtc(
            this ModelBuilder modelBuilder,
            IModelStore modelStore)
        {
            return HasIndex(modelBuilder, modelStore, typeof(IUpdatedUtc), nameof(IUpdatedUtc.UpdatedUtc));
        }
        
        public static ModelBuilder HasIndexDeletable(
            this ModelBuilder modelBuilder,
            IModelStore modelStore)
        {
            return HasIndex(modelBuilder, modelStore, typeof(IDeletable), nameof(IDeletable.DeletedUtc));
        }

        public static ModelBuilder UseValueConverterForType<TModel, TProvider>(
            this ModelBuilder modelBuilder,
            ValueConverter<TModel, TProvider> converter)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            return UseValueConverterForType(modelBuilder, typeof(TModel), converter);
        }
        
        private static ModelBuilder HasIndex(
            ModelBuilder modelBuilder,
            IModelStore modelStore,
            Type interfaceType,
            string propertyName)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            if (modelStore == null) throw new ArgumentNullException(nameof(modelStore));

            foreach (var model in modelStore.GetModels())
            {
                if (model.GetInterfaces().Any(@interface => @interface == interfaceType))
                {
                    modelBuilder.Entity(model).HasIndex(propertyName);
                }
            }

            return modelBuilder;
        }

        private static ModelBuilder UseValueConverterForType(
            ModelBuilder modelBuilder,
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