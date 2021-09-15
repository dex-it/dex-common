using System.Threading;
using System.Threading.Tasks;
using Neo4jClient.Transactions;

namespace Dex.Neo4J
{
    public interface IGraphFTIndexProvider
    {
        Task RegisterFTIndexes(ITransactionalGraphClient graphClient, CancellationToken cancellationToken);
    }
}