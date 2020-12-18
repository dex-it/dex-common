using Dex.Lock.Async.Impl;
using System;
using System.Threading.Tasks;

namespace Dex.Lock.Database
{
    public sealed class DbLockReleaser : IAsyncDisposable, IDisposable
    {
        private readonly DatabaseAsyncLock _asyncLock;
        private readonly string _tableName;

        internal DbLockReleaser(DatabaseAsyncLock asyncLock, string tableName)
        {
            _asyncLock = asyncLock;
            _tableName = tableName;
        }

        public ValueTask DisposeAsync()
        {
            var task = _asyncLock.RemoveLockObjectAsync(_tableName);
            return new ValueTask(task);
        }

        public void Dispose()
        {
            _asyncLock.RemoveLockObject(_tableName);
        }
    }
}