using System;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Dex.DataProvider.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Provider
{
    public class EfRoDataProvider : IRoDataProvider
    {
        protected DbContext DbContext { get; }

        public EfRoDataProvider(DbContext connection)
        {
            DbContext = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public TransactionScope Transaction()
        {
            return Transaction(IsolationLevel.ReadCommitted);
        }

        public TransactionScope Transaction(IsolationLevel isolationLevel)
        {
            var ambientLevel = System.Transactions.Transaction.Current?.IsolationLevel;
            var txOptions = new TransactionOptions
            {
                IsolationLevel = ambientLevel == null
                    ? isolationLevel
                    : (IsolationLevel) Math.Min((int) ambientLevel, (int) isolationLevel)
            };

            return new TransactionScope(
                TransactionScopeOption.Required,
                txOptions,
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public IQueryable<T> Get<T>() 
            where T : class
        {
            return DbContext.Set<T>().AsQueryable().AsNoTracking();
        }

        public IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate) 
            where T : class
        {
            return DbContext.Set<T>().AsQueryable().Where(predicate).AsNoTracking();
        }
        
        
        void IRoDataProvider.Reset()
        {
            DbContext.ChangeTracker.Clear();
        }
    }
}