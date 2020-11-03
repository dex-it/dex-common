using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLock
    {
        Task LockAsync(Func<Task> asyncAction);
        Task LockAsync(Action action);
        ValueTask<LockReleaser> LockAsync();
    }
}