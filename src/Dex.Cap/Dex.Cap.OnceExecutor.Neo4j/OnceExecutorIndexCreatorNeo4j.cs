using System;
using System.Threading.Tasks;
using Dex.Neo4J;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public static class OnceExecutorIndexCreatorNeo4j
    {
        public static Task RegisterStepExecutorIndexes(this ITransactionalGraphClient graphClient)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));

            return Task.WhenAll(
                graphClient.CreateIndex<LastTransaction, Guid>(arg => arg.IdempotentKey),
                graphClient.CreateIndex<LastTransaction, DateTime>(arg => arg.Created)
            );
        }
    }
}