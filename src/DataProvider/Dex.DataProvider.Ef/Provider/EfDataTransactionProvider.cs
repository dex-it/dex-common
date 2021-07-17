using System;
using System.Transactions;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Ef.Transactions;
using Dex.DataProvider.Providers;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Provider
{
    public class EfDataTransactionProvider : DataTransactionScopeProvider
    {
        protected DbContext DbContext { get; }

        public EfDataTransactionProvider(DbContext connection)
        {
            DbContext = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <inheritdoc />
        public override IDataTransaction Transaction(IsolationLevel isolationLevel)
        {
            var dataTransaction = base.Transaction(isolationLevel);
            return new EfDecoratorDataTransaction(dataTransaction, DbContext);
        }
    }
}