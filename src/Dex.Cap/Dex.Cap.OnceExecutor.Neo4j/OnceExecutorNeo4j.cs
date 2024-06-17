using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.Models;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    // ReSharper disable once InconsistentNaming
    public class OnceExecutorNeo4j : BaseOnceExecutor<INeo4jOptions, ITransactionalGraphClient>
    {
        protected override ITransactionalGraphClient Context { get; }

        public OnceExecutorNeo4j(ITransactionalGraphClient graphClient)
        {
            Context = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            INeo4jOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            ArgumentNullException.ThrowIfNull(operation);

            TResult? result;
            var t = Context.BeginTransaction();

            try
            {
                result = await operation(cancellationToken).ConfigureAwait(false);
                await t.CommitAsync().ConfigureAwait(false);
            }
            finally
            {
                t.Dispose();
            }

            return result;
        }

        protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            var lastTransaction = await Context.Cypher
                .Match($"(t:{nameof(LastTransaction)})")
                .Where((LastTransaction t) => t.IdempotentKey == idempotentKey)
                .Return(t => t.As<LastTransaction>())
                .ResultsAsync.ConfigureAwait(false);

            return lastTransaction.Any();
        }

        protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            await Context.Cypher
                .Create($"(last:{nameof(LastTransaction)}" + " {lt})")
                .WithParam("lt", new LastTransaction { IdempotentKey = idempotentKey })
                .ExecuteWithoutResultsAsync().ConfigureAwait(false);
        }
    }
}