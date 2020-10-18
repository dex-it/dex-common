using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProvider<in T>
    {
        IAsyncLock GetLock(T key);

        Task<bool> RemoveLock(T key);
    }
}