using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.DataProvider.Contracts
{
    public interface ISafeExecuteProvider
    {
        Task<T> SafeExecuteAsync<T>(IDataProvider provider,
            Func<IDataProvider, CancellationToken, Task<T>> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken token = default);

        Task SafeExecuteAsync(IDataProvider provider,
            Func<IDataProvider, CancellationToken, Task> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken token = default);
    }
}