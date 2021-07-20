using System.Transactions;

namespace Dex.DataProvider.Contracts
{
    public interface IDataTransactionProvider
    {
        /// <summary>
        /// Starts a new transaction.
        /// </summary>
        /// <returns>
        ///      A <see cref="IDataTransaction" /> that represents the started transaction.
        /// </returns>
        IDataTransaction BeginTransaction();
        
        /// <summary>
        /// Starts a new transaction.
        /// </summary>
        /// <param name="isolationLevel">
        ///     A <see cref="IsolationLevel" /> enumeration that specifies the isolation level of the transaction.
        /// </param>
        /// <returns>
        ///     A <see cref="IDataTransaction" /> that represents the started transaction.
        /// </returns>
        IDataTransaction BeginTransaction(IsolationLevel isolationLevel);
    }
}