using System;
using System.Data;
using Dex.Lock.Async;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLockProvider : BaseLockProvider<DbLockReleaser>
    {
        private readonly IDbTransaction _dbTransaction;
        private IDbConnection DbConnection => _dbTransaction.Connection;
        public override string InstanceKey { get; }

        public DatabaseAsyncLockProvider(IDbTransaction dbTransaction, string instanceId)
        {
            _dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));
            InstanceKey = CreateKey(instanceId);
        }

        public override IAsyncLock<DbLockReleaser> GetLocker(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return new DatabaseAsyncLock(DbConnection, CreateKey(key));
        }
    }
}