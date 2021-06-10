using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.DataProvider.Contracts
{
    public interface IRetryTransactionProvider
    {
        Task<T> Execute<T, TArg>(
            IDataTransactionProvider provider,
            Func<TArg, CancellationToken, Task<T>> func,
            TArg arg,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default);
        
        Task<T> Execute<T>(
            IDataTransactionProvider provider,
            Func<CancellationToken, Task<T>> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default);
        
        Task Execute<TArg>(
            IDataTransactionProvider provider,
            Func<TArg, CancellationToken, Task> func,
            TArg arg,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default);
        
        Task Execute(
            IDataTransactionProvider provider,
            Func<CancellationToken, Task> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default);
    }
}