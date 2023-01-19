using Dex.Cap.OnceExecutor;

namespace Dex.Cap.Ef.Tests.Strategies
{
    public interface IConcrete1ExecutionStrategy : IOnceExecutionStrategy<string, string>
    {
    }
}