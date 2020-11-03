using System.Threading.Tasks;
using Dex.Lock.Async.Impl;

namespace Dex.Lock.Async
{
    public interface IAsyncLock
    {
        ValueTask<LockReleaser> LockAsync();
    }
}