using System;
using System.Linq;
using Dex.DataProvider.Contracts.Entities;
using Dex.DataProvider.Ef.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Contexts
{
    public abstract class InsertSystemPropertiesDbContext : ModelDbContext
    {
        protected InsertSystemPropertiesDbContext(IModelStore modelStore)
            : base(modelStore)
        {
            Init();
        }

        protected InsertSystemPropertiesDbContext(IModelStore modelStore, DbContextOptions options)
            : base(modelStore, options)
        {
            Init();
        }

        private void Init()
        {
            SavingChanges += OnSavingChanges;
        }
        
        protected virtual void OnSavingChanges(object sender, SavingChangesEventArgs e)
        {
            var now = DateTime.UtcNow;
            var entries = ChangeTracker.Entries()
                .Where(static e => e.State is EntityState.Modified or EntityState.Added);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added && entityEntry.Entity is ICreatedUtc createdEntity)
                {
                    createdEntity.CreatedUtc = now;
                }

                if (entityEntry.Entity is IUpdatedUtc updatedEntity)
                {
                    updatedEntity.UpdatedUtc = now;
                }
            }
        }
    }
}