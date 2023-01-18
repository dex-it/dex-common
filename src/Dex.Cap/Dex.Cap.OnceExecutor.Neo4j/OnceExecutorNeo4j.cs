using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    // ReSharper disable once InconsistentNaming
    public class OnceExecutorNeo4j : BaseOnceExecutor<ITransactionalGraphClient>
    {
        protected override ITransactionalGraphClient Context { get; }

        public OnceExecutorNeo4j(ITransactionalGraphClient graphClient)
        {
            Context = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        }

        protected override async Task<TResult?> ExecuteInTransaction<TResult>(string idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken)
            where TResult : default
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

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

        protected override Task OnModificationCompleted(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecuted(string idempotentKey, CancellationToken cancellationToken)
        {
            var lastTransaction = await Context.Cypher
                .Match($"(t:{nameof(LastTransaction)})")
                .Where((LastTransaction t) => t.IdempotentKey == idempotentKey)
                .Return(t => t.As<LastTransaction>())
                .ResultsAsync.ConfigureAwait(false);

            return lastTransaction.Any();
        }

        protected override async Task SaveIdempotentKey(string idempotentKey, CancellationToken cancellationToken)
        {
            await Context.Cypher
                .Create($"(last:{nameof(LastTransaction)}" + " {lt})")
                .WithParam("lt", new LastTransaction { IdempotentKey = idempotentKey })
                .ExecuteWithoutResultsAsync().ConfigureAwait(false);
        }
    }
}