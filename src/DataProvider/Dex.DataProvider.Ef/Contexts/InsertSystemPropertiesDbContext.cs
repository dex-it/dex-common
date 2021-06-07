using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        }

        protected InsertSystemPropertiesDbContext(IModelStore modelStore, DbContextOptions options)
            : base(modelStore, options)
        {
        }

        public override int SaveChanges()
        {
            InsertSystemProperties();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            InsertSystemProperties();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            InsertSystemProperties();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            InsertSystemProperties();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void InsertSystemProperties()
        {
            var now = DateTime.Now;
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