using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public interface IOnceExecutorNeo4j<TResult> : IOnceExecutor<ITransactionalGraphClient, TResult>
    {
    }
}