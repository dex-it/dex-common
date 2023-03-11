using System;
using System.Transactions;

namespace Dex.Cap.Common.Ef.Helpers
{
    public static class TransactionScopeHelper
    {
        public static TransactionScope CreateTransactionScope(TransactionScopeOption option, IsolationLevel isolationLevel)
        {
            return CreateTransactionScope(option, isolationLevel, TimeSpan.FromSeconds(60));
        }

        public static TransactionScope CreateTransactionScope(TransactionScopeOption option, IsolationLevel isolationLevel, TimeSpan timeout)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout
            };

            return new TransactionScope(option, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}