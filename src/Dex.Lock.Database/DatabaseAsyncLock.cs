using System;
using System.Data;
using System.Data.Common;
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

        private async Task<DbLockReleaser> InternalLockAsync()
        {
            var tableName = CreateTableName();
            await ExecuteCommandAsync($"CREATE TABLE {tableName} (CODE integer);").ConfigureAwait(false);
            return new DbLockReleaser(this, tableName);
        }

        internal Task RemoveLockObjectAsync(string tableName)
        {
            return ExecuteCommandAsync($"DROP TABLE {tableName};");
        }

        internal void RemoveLockObject(string tableName)
        {
            ExecuteCommand($"DROP TABLE {tableName};");
        }

        //--
        
        private string CreateTableName()
        {
            return $"LT_{_key}";
        }

        private async Task ExecuteCommandAsync(string command)
        {
            using var cmd = _dataConnection.CreateCommand();
            cmd.CommandText = command;

            if (cmd is DbCommand dbCommand)
            {
                await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            else
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void ExecuteCommand(string command)
        {
            using var cmd = _dataConnection.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }
    }
}