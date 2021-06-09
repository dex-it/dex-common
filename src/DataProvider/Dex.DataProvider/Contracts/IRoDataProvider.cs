using System;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace Dex.DataProvider.Contracts
{
    public interface IRoDataProvider
    {
        TransactionScope Transaction();
        TransactionScope Transaction(IsolationLevel isolationLevel);

        IQueryable<T> Get<T>() 
            where T : class;
        
        IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate) 
            where T : class;
        
        internal void Reset();
    }
}