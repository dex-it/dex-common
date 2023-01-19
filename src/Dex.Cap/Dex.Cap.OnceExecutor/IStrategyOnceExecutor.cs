using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    // ReSharper disable once UnusedTypeParameter
    public interface IStrategyOnceExecutor<in TArg, TResult, TExecutionStrategy>
        where TExecutionStrategy : IOnceExecutionStrategy<TArg, TResult>
    {
        Task<TResult?> ExecuteAsync(TArg arg, CancellationToken cancellationToken = default);
    }
}