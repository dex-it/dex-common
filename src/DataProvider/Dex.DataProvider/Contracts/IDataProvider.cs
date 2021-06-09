using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.DataProvider.Contracts.Entities;

namespace Dex.DataProvider.Contracts
{
    public interface IDataProvider : IRoDataProvider
    {
        Task<T> Insert<T>(T entity, CancellationToken cancellationToken = default) 
            where T : class;
        
        Task BatchInsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) 
            where T : class;

        Task<T> Update<T>(T entity, CancellationToken cancellationToken = default) 
            where T : class;
        
        Task BatchUpdate<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) 
            where T : class;

        Task Delete<T>(T entity, CancellationToken cancellationToken = default) 
            where T : class;
        
        Task BatchDelete<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) 
            where T : class;

        Task DeleteById<T, TKey>(TKey id, CancellationToken cancellationToken = default) 
            where T : class, IEntity<TKey> 
            where TKey : IComparable;
        
        Task BatchDeleteByIds<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) 
            where T : class, IEntity<TKey> 
            where TKey : IComparable;

        Task SetDelete<T, TKey>(TKey id, CancellationToken cancellationToken = default) 
            where T : class, IDeletable, IEntity<TKey> 
            where TKey : IComparable;
        
        Task BatchSetDelete<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) 
            where T : class, IDeletable, IEntity<TKey> 
            where TKey : IComparable;
    }
}