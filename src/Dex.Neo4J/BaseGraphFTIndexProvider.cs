using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;

namespace Dex.Neo4J
{
    // ReSharper disable once InconsistentNaming
    public abstract class BaseGraphFTIndexProvider : IGraphFTIndexProvider
    {
        public async Task RegisterFTIndexes(ITransactionalGraphClient graphClient, CancellationToken cancellationToken)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));

            try
            {
                await CreateIndexes(graphClient, cancellationToken).ConfigureAwait(false);
            }
            catch (NeoException e) when (e.NeoMessage.StartsWith("There already exists an index", StringComparison.OrdinalIgnoreCase))
            {
                // nothing to do
            }
        }

        protected static async Task SafeCreateIndex(ITransactionalGraphClient graphClient, Func<ICypherFluentQuery, ICypherFluentQuery> query,
            CancellationToken cancellationToken)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));
            if (query == null) throw new ArgumentNullException(nameof(query));

            try
            {
                await query(graphClient.Cypher).ExecuteWithoutResultsAsync().ConfigureAwait(false);
            }
            catch (NeoException e) when (e.NeoMessage.StartsWith("There already exists an index", StringComparison.OrdinalIgnoreCase))
            {
                // nothing to do
            }
        }

        protected abstract Task CreateIndexes(ITransactionalGraphClient graphClient, CancellationToken cancellationToken);

        protected static async Task ExecuteBatch(ICypherFluentQuery<int> query, int batchSize, CancellationToken cancellationToken)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            while (!cancellationToken.IsCancellationRequested)
            {
                var count = await query.ResultsAsync.ConfigureAwait(false);
                if (count.First() != batchSize) break;
            }
        }
    }
}