using System.Transactions;

namespace Dex.DataProvider.Contracts
{
    public interface IDataTransactionProvider
    {
        IDataTransaction Transaction();
        IDataTransaction Transaction(IsolationLevel isolationLevel);
    }
}