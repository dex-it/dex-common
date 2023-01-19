using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;

namespace Dex.Cap.Ef.Tests.Strategies
{
    public interface IConcrete3ExecutionStrategy : IOnceExecutionStrategy<string, TestUser>
    {
    }
}