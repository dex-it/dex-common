using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public interface INeo4jOnceExecutor<TResult> : IOnceExecutor<ITransactionalGraphClient, TResult>
    {
    }
}