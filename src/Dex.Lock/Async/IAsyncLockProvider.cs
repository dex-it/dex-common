using System;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProvider<in T, TR> where TR : IDisposable
    {
        /// <summary>
        /// Get async locker object by Key, locker with equal key are equal.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IAsyncLock<TR> GetLocker(T key);
    }
}