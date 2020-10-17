using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLock
    {
        Task LockAsync([NotNull] Func<Task> asyncAction);
        Task LockAsync([NotNull] Action action);
    }
}