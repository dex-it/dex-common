using System;
using System.Linq;
using System.Linq.Expressions;

namespace Dex.DataProvider.Contracts
{
    public interface IRoDataProvider : IDataTransactionProvider
    {
        IQueryable<T> Get<T>() 
            where T : class;
        
        IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate) 
            where T : class;
    }
}