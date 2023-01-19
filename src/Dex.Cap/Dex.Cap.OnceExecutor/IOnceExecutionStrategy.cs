using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutionStrategy<in TArg, TResult>
    {
        IsolationLevel TransactionIsolationLevel => IsolationLevel.ReadCommitted;

        Task<bool> CheckIdempotenceAsync(TArg argument, CancellationToken cancellationToken);

        Task ExecuteAsync(TArg argument, CancellationToken cancellationToken);

        Task<TResult?> ReadAsync(TArg argument, CancellationToken cancellationToken);
    }
}