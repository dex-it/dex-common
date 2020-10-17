using System.Threading;

namespace Dex.Lock.RwLock
{
    public static class ReaderWriterLockSlimExtension
    {
        public static LockScope GetReadLockScope(this ReaderWriterLockSlim locker)
        {
            return new LockScope(locker, LockScope.LockMode.Read);
        }

        public static LockScope GetUpgradableLockScope(this ReaderWriterLockSlim locker)
        {
            return new LockScope(locker, LockScope.LockMode.Upgrade);
        }

        public static LockScope GetWriteLockScope(this ReaderWriterLockSlim locker)
        {
            return new LockScope(locker, LockScope.LockMode.Write);
        }
    }
}