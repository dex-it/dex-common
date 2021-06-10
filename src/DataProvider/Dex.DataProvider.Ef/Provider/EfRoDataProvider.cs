using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Providers;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Provider
{
    public class EfRoDataProvider : DataTransactionScopeProvider, IRoDataProvider
    {
        protected DbContext DbContext { get; }

        public EfRoDataProvider(DbContext connection)
        {
            DbContext = connection ?? throw new ArgumentNullException(nameof(connection));
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