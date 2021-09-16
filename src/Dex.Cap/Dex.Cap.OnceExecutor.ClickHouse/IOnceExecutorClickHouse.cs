using Octonica.ClickHouseClient;

namespace Dex.Cap.OnceExecutor.ClickHouse
{
    public interface IOnceExecutorClickHouse<TResult> : IOnceExecutor<ClickHouseConnection, TResult>
    {
    }
}