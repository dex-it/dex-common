using System;
using System.Linq;
using System.Reflection;
using Dex.Ef.Comparer;
using Microsoft.EntityFrameworkCore;
using Dex.Ef.Contracts;
using Dex.Ef.Provider;
using IndexAttribute = Dex.Ef.Attributes.IndexAttribute;

namespace Dex.Ef.Extension
{
    public static class BuilderExtension
    {
        public static ModelBuilder BuildIndex(
            this ModelBuilder builder, 
            IModelStore modelStore,
            IIndexProvider? indexProvider = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (modelStore == null) throw new ArgumentNullException(nameof(modelStore));
            indexProvider ??= new DefaultIndexProvider();

            foreach (var type in modelStore.GetModels())
            {
                var indices = type.GetProperties()
                    .Select(p => new {Property = p, Attribute = p.GetCustomAttribute<IndexAttribute>(true)})
                    .Where(x => x.Attribute != null)
                    .Select(p => new
                    {
                        IndexName = string.IsNullOrWhiteSpace(p.Attribute.IndexName)
                            ? null
                            : p.Attribute.IndexName,
                        PropertyName = p.Property.Name,
                        Order = p.Attribute.Order,
                        Attribute = p.Attribute
                    })
                    .GroupBy(p => p.IndexName, NullUniqueEqualityComparer<string?>.Get());
                
                foreach (var index in indices)
                {
                    var attribute = index.First().Attribute;

                    var propertyNames = index.OrderBy(x=>x.Order).Select(x=>x.PropertyName).ToArray();
                    var indexBuilder = builder.Entity(type)
                        .HasIndex(propertyNames, attribute.IndexName)
                        .IsUnique(attribute.IsUnique);

                    if (!string.IsNullOrEmpty(attribute.Method))
                    {
                        indexProvider.HasMethod(indexBuilder, attribute.Method);
                    }
                }
            }

            return builder;
        }
    }
}