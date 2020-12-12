using System;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProvider<in T, TR> where TR : IDisposable
    {
        IAsyncLock<TR> GetLocker(T key);

        Task<bool> RemoveLocker(T key);
    }
}