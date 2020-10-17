using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProvider<in T>
    {
        IAsyncLock Get(T key);

        Task<bool> Remove(T key);
    }
}