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

        public async ValueTask DisposeAsync()
        {
            await _asyncLock.RemoveLockObjectAsync(_tableName).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _asyncLock.RemoveLockObject(_tableName);
        }
    }
}