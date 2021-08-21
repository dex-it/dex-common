﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Dex.Lock.RwLock
{
    public class LockScope : IDisposable
    {
        private bool _isDisposed;
        private readonly ReaderWriterLockSlim _locker;
        private readonly LockMode _lmMode;

        internal enum LockMode
        {
            Read,
            Upgrade,
            Write
        }

        internal LockScope([NotNull] ReaderWriterLockSlim locker, LockMode lmMode)
        {
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _lmMode = lmMode;
            _isDisposed = false;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing) return;
            
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
            }

            _isDisposed = true;
        }
    }
}