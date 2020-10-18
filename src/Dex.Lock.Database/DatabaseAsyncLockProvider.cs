using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dex.Lock.Async;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLockProvider<T> : IAsyncLockProvider<T>
    {
        private readonly IDbConnection _dbConnection;
        private readonly string _instanceId;

        internal DatabaseAsyncLockProvider([NotNull] IDbConnection dbConnection, string instanceId)
        {
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            _instanceId = instanceId ?? Guid.NewGuid().ToString("N");
        }

        public IAsyncLock Get([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return new DatabaseAsyncLock(_dbConnection, CreateKey(key));
        }

        public Task<bool> Remove([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var databaseAsyncLock = new DatabaseAsyncLock(_dbConnection, CreateKey(key));
            databaseAsyncLock.RemoveLockObject(CreateKey(key));
            return Task.FromResult(true);
        }

        private string CreateKey(T key)
        {
            return _instanceId + key.ToString().Replace("-", "", StringComparison.InvariantCulture);
        }
    }
}