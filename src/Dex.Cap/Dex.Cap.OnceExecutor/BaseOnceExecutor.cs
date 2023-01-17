using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TArg, TDbContext> : IOnceExecutor<TArg, TDbContext>, IOnceExecutor<TArg>
    {
        protected abstract TDbContext Context { get; }

        public async Task<TResult?> Execute<TResult>(
            TArg arg,
            Func<TDbContext, CancellationToken, Task> modificator,
            Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            CancellationToken cancellationToken = default)
        {
            var result = await ExecuteInTransaction(arg, async (token) =>
            {
                if (!await IsAlreadyExecuted(arg, token))
                {
                    await BeforeModification(arg, token);
                    await modificator(Context, token);
                    await AfterModification(arg, token);
                }

                var result = selector != null
                    ? await selector(Context, token)
                    : default;

                return result;
            }, cancellationToken);

            return result;
        }

        public Task Execute(TArg arg, Func<TDbContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return Execute<int>(arg, modificator, null, cancellationToken);
        }

        public Task<TResult?> Execute<TResult>(TArg arg,
            Func<CancellationToken, Task> modificator, Func<CancellationToken, Task<TResult?>> selector,
            CancellationToken cancellationToken = default)
        {
            return Execute<TResult>(arg, (_, token) => modificator(token), (_, token) => selector(token), cancellationToken: cancellationToken);
        }

        public Task Execute(TArg arg, Func<CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return Execute(arg, (_, token) => modificator(token), cancellationToken: cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransaction<TResult>(TArg arg, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecuted(TArg arg, CancellationToken cancellationToken);

        protected virtual Task BeforeModification(TArg arg, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterModification(TArg arg, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}