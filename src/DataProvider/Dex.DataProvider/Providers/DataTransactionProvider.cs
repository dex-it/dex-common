using System;
using System.Transactions;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Transactions;

namespace Dex.DataProvider.Providers
{
    public class DataTransactionScopeProvider : IDataTransactionProvider
    {
        public virtual IDataTransaction Transaction()
        {
            return Transaction(IsolationLevel.ReadCommitted);
        }

        public virtual IDataTransaction Transaction(IsolationLevel isolationLevel)
        {
            var ambientLevel = System.Transactions.Transaction.Current?.IsolationLevel;
            var txOptions = new TransactionOptions
            {
                IsolationLevel = ambientLevel == null
                    ? isolationLevel
                    : (IsolationLevel) Math.Min((int) ambientLevel, (int) isolationLevel)
            };

            var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                txOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            return new TransactionScopeToDataTransactionAdapter(transactionScope);
        }
    }
}