using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Dex.Lock.RwLock
{
    public class LockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _locker;
        private readonly LockMode _lmMode;

        internal enum LockMode
        {
            Read, Upgrade, Write
        }

        internal LockScope([NotNull] ReaderWriterLockSlim locker, LockMode lmMode)
        {
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _lmMode = lmMode;

            switch (lmMode)
            {
                case LockMode.Read:
                    _locker.EnterReadLock();
                    break;
                case LockMode.Upgrade:
                    _locker.EnterUpgradeableReadLock();
                    break;
                case LockMode.Write:
                    _locker.EnterWriteLock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lmMode), lmMode, null);
            }
        }

        public void Dispose()
        {
            switch (_lmMode)
            {
                case LockMode.Read:
                    _locker.ExitReadLock();
                    break;
                case LockMode.Upgrade:
                    _locker.ExitUpgradeableReadLock();
                    break;
                case LockMode.Write:
                    _locker.ExitWriteLock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
