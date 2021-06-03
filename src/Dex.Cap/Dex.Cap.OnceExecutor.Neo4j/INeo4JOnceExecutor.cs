using MC.Core.Consistent.OnceExecutor;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public interface INeo4JOnceExecutor<TResult> : IOnceExecutor<ITransactionalGraphClient, TResult>
    {
    }
}