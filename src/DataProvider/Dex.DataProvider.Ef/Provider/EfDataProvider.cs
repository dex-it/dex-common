using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Provider
{
    public sealed class EfDataProvider : EfRoDataProvider, IDataProvider
    {
        private readonly IDataExceptionManager _exceptionManager;

        public EfDataProvider(DbContext connection, IDataExceptionManager exceptionManager)
            : base(connection)
        {
            _exceptionManager = exceptionManager ?? throw new ArgumentNullException(nameof(exceptionManager));
        }

        public Task<T> Insert<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                async static(dbContext, state) =>
                {
                    var entityEntry = dbContext.Set<T>().Add(state.entity);
                    await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                    return entityEntry.Entity;
                },
                (entity, cancellationToken));
        }

        public Task BatchInsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                static(dbContext, state) =>
                {
                    foreach (var entity in state.entities)
                    {
                        dbContext.Set<T>().Add(entity);
                    }

                    return dbContext.SaveChangesAsync(state.cancellationToken);
                },
                (entities, cancellationToken));
        }

        public Task<T> Update<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                static async (dbContext, state) =>
                {
                    var entityEntry = dbContext.Update(state.entity);
                    await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                    return entityEntry.Entity;
                },
                (entity, cancellationToken));
        }

        public Task BatchUpdate<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                static(dbContext, state) =>
                {
                    dbContext.UpdateRange(state.entities);
                    return dbContext.SaveChangesAsync(state.cancellationToken);
                },
                (entities, cancellationToken));
        }

        public Task Delete<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                static(dbContext, state) =>
                {
                    dbContext.Set<T>().Remove(state.entity);
                    return dbContext.SaveChangesAsync(state.cancellationToken);
                },
                (entity, cancellationToken));
        }

        public Task BatchDelete<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteCommand(
                static(dbContext, state) =>
                {
                    dbContext.Set<T>().RemoveRange(state.entities);
                    return dbContext.SaveChangesAsync(state.cancellationToken);
                },
                (entities, cancellationToken));
        }

        public Task DeleteById<T, TKey>(TKey id, CancellationToken cancellationToken = default)
            where T : class, IEntity<TKey>
            where TKey : IComparable
        {
            return ExecuteCommand(
                async static(dbContext, state) =>
                {
                    var entity = await dbContext.Set<T>()
                        .Where(t => state.id.Equals(t.Id))
                        .SingleAsync(state.cancellationToken)
                        .ConfigureAwait(false);
                    dbContext.Set<T>().Remove(entity);
                    return await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                },
                (id, cancellationToken));
        }

        public Task BatchDeleteByIds<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
            where T : class, IEntity<TKey>
            where TKey : IComparable
        {
            return ExecuteCommand(
                async static(dbContext, state) =>
                {
                    var entity = await dbContext.Set<T>()
                        .Where(t => state.ids.Contains(t.Id))
                        .ToArrayAsync(state.cancellationToken)
                        .ConfigureAwait(false);
                    dbContext.Set<T>().RemoveRange(entity);
                    return await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                },
                (ids, cancellationToken));
        }

        public Task SetDelete<T, TKey>(TKey id, CancellationToken cancellationToken = default)
            where T : class, IDeletable, IEntity<TKey>
            where TKey : IComparable
        {
            return ExecuteCommand(
                async static(dbContext, state) =>
                {
                    var entity = await dbContext.Set<T>()
                        .Where(t => state.id.Equals(t.Id))
                        .SingleAsync(state.cancellationToken)
                        .ConfigureAwait(false);

                    entity.DeletedUtc = DateTime.UtcNow;
                    dbContext.Update(entity);

                    return await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                },
                (id, cancellationToken));
        }

        public Task BatchSetDelete<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
            where T : class, IDeletable, IEntity<TKey>
            where TKey : IComparable
        {
            return ExecuteCommand(
                async static(dbContext, state) =>
                {
                    object[] entities = await dbContext.Set<T>()
                        .Where(t => state.ids.Contains(t.Id))
                        .ToArrayAsync(state.cancellationToken)
                        .ConfigureAwait(false);

                    dbContext.UpdateRange(entities);
                    return await dbContext.SaveChangesAsync(state.cancellationToken).ConfigureAwait(false);
                },
                (ids, cancellationToken));
        }

        private async Task<T> ExecuteCommand<T, TState>(Func<DbContext, TState, Task<T>> func, TState state)
        {
            try
            {
                return await func(DbContext, state);//.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw _exceptionManager.Normalize(exception);
            }
        }
    }
}