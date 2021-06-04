using System;
using System.Linq;
using System.Threading.Tasks;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public class Neo4JOnceExecutor<TResult> : BaseOnceExecutor<ITransactionalGraphClient, TResult>, INeo4JOnceExecutor<TResult>
    {
        private ITransaction _transaction;
        protected override ITransactionalGraphClient Context { get; }

        public Neo4JOnceExecutor(ITransactionalGraphClient graphClient)
        {
            Context = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        }

        protected override Task OnModificationComplete()
        {
            return Task.CompletedTask;
        }

        protected override IDisposable BeginTransaction()
        {
            return _transaction = Context.BeginTransaction();
        }

        protected override Task CommitTransaction()
        {
            return _transaction.CommitAsync();
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey)
        {
            var lastTransaction = await Context.Cypher
                .Match($"(t:{nameof(LastTransaction)})")
                .Where((LastTransaction t) => t.IdempotentKey == idempotentKey)
                .Return(t => t.As<LastTransaction>())
                .ResultsAsync;

            return lastTransaction.Any();
        }

        protected override async Task SaveIdempotentKey(Guid idempotentKey)
        {
            await Context.Cypher
                .Create($"(last:{nameof(LastTransaction)}" + " {lt})")
                .WithParam("lt", new LastTransaction {IdempotentKey = idempotentKey})
                .ExecuteWithoutResultsAsync();
        }
    }
}