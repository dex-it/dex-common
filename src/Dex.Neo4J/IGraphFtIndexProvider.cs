using System.Threading;
using System.Threading.Tasks;
using Neo4jClient.Transactions;

namespace Dex.Neo4J
{
    public interface IGraphFtIndexProvider
    {
        Task RegisterFtIndexes(ITransactionalGraphClient graphClient, CancellationToken cancellationToken);
    }
}