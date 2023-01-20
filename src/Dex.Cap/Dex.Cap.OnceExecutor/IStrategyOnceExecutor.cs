using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IStrategyOnceExecutor<in TArg, TResult>
    {
        Task<TResult?> ExecuteAsync(TArg argument, CancellationToken cancellationToken = default);
    }
}