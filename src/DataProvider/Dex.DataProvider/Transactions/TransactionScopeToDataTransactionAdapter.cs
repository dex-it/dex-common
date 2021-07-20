using System;
using System.Transactions;
using Dex.DataProvider.Contracts;

namespace Dex.DataProvider.Transactions
{
    public sealed class TransactionScopeToDataTransactionAdapter : IDataTransaction
    {
        private readonly TransactionScope _transactionScope;

        public TransactionScopeToDataTransactionAdapter(TransactionScope transactionScope)
        {
            _transactionScope = transactionScope ?? throw new ArgumentNullException(nameof(transactionScope));
        }

        /// <inheritdoc />
        public void Complete()
        {
            _transactionScope.Complete();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _transactionScope.Dispose();
        }
    }
}