using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.OnceExecutor.ClickHouse
{
    [SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
    public interface IClickHouseOptions : IOnceExecutorOptions
    {
    }
}