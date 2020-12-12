using System;

namespace Dex.Lock.Database
{
    public sealed class DbLockReleaser : IDisposable
    {
        private readonly DatabaseAsyncLock _asyncLock;
        private readonly string _tableName;

        internal DbLockReleaser(DatabaseAsyncLock asyncLock, string tableName)
        {
            _asyncLock = asyncLock;
            _tableName = tableName;
        }


        public void Dispose()
        {
            _asyncLock.RemoveLockObject(_tableName);
        }
    }
}