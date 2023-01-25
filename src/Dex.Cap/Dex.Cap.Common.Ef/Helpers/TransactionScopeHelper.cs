using System.Transactions;

namespace Dex.Cap.Common.Ef.Helpers
{
    public static class TransactionScopeHelper
    {
        public static TransactionScope CreateTransactionScope(TransactionScopeOption option, IsolationLevel isolationLevel)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel
            };

            return new TransactionScope(option, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}