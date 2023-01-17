using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<in TArg, out TContext>
    {
        Task<TResult?> Execute<TResult>(TArg arg, Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult?>> selector, CancellationToken cancellationToken = default);

        Task Execute(TArg arg, Func<TContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default);
    }

    public interface IOnceExecutor<in TArg>
    {
        Task<TResult?> Execute<TResult>(TArg arg, Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector, CancellationToken cancellationToken = default);

        Task Execute(TArg arg, Func<CancellationToken, Task> modificator, CancellationToken cancellationToken = default);
    }
}