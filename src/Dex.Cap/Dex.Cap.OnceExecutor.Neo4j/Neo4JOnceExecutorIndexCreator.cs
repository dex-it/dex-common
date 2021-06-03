using System;
using System.Threading.Tasks;
using Dex.Neo4J;
using MC.Core.Consistent.OnceExecutor;
using Neo4jClient.Transactions;

namespace Dex.Cap.OnceExecutor.Neo4j
{
    public static class Neo4JOnceExecutorIndexCreator
    {
        public static Task RegisterStepExecutorIndexes(this ITransactionalGraphClient graphClient)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));

            return Task.WhenAll(
                graphClient.CreateIndex<LastTransaction, Guid>(arg => arg.Last),
                graphClient.CreateIndex<LastTransaction, DateTime>(arg => arg.Created)
            );
        }
    }
}