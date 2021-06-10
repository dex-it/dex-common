using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.DataProvider.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Provider
{
    public class EfRoDataProvider : EfDataTransactionProvider, IRoDataProvider
    {
        public EfRoDataProvider(DbContext connection) 
            : base(connection)
        {
        }

        public virtual IQueryable<T> Get<T>() 
            where T : class
        {
            return DbContext.Set<T>().AsQueryable().AsNoTracking();
        }

        public virtual IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate) 
            where T : class
        {
            return DbContext.Set<T>().AsQueryable().Where(predicate).AsNoTracking();
        }
    }
}