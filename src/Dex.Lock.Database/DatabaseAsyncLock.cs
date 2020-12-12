using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Dex.Lock.Async;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLock : IAsyncLock<DbLockReleaser>
    {
        private readonly IDbConnection _dataConnection;
        private readonly string _key;

        // TODO check database for support, dirty connection close, Danilov
        internal DatabaseAsyncLock(IDbConnection dataConnection, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _dataConnection = dataConnection ?? throw new ArgumentNullException(nameof(dataConnection));
            _key = key.ToUpper(CultureInfo.InvariantCulture);
        }

        public ValueTask<DbLockReleaser> LockAsync()
        {
            #pragma warning disable CA2000
            return new ValueTask<DbLockReleaser>(InternalLockAsync());
        }

        private Task<DbLockReleaser> InternalLockAsync()
        {
            var tableName = CreateTableName();
            ExecuteCommand($"CREATE TABLE {tableName} (CODE integer);");
            return Task.FromResult(new DbLockReleaser(this, tableName));
        }

        internal void RemoveLockObject(string tableName)
        {
            ExecuteCommand($"DROP TABLE {tableName};");
        }

        private string CreateTableName()
        {
            return $"LT_{_key}";
        }

        private void ExecuteCommand(string command)
        {
            using var cmd = _dataConnection.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }
    }
}