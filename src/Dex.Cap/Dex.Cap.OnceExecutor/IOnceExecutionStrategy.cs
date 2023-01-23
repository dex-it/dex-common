using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.Cap.OnceExecutor
{
    /// <summary>
    /// Once execution strategy contract
    /// </summary>
    public interface IOnceExecutionStrategy<in TArg, TResult>
    {
        IsolationLevel TransactionIsolationLevel => IsolationLevel.ReadCommitted;

        /// <summary>
        /// Checks that the execution has already been completed
        /// </summary>
        Task<bool> IsAlreadyExecutedAsync(TArg argument, CancellationToken cancellationToken);

        /// <summary>
        /// The action being performed
        /// </summary>
        Task ExecuteAsync(TArg argument, CancellationToken cancellationToken);

        /// <summary>
        /// Return value (if necessary)
        /// </summary>
        Task<TResult?> ReadAsync(TArg argument, CancellationToken cancellationToken);
    }
}