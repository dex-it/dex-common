using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Dex.Lock.Async;
using Dex.Lock.Async.Impl;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLock : IAsyncLock
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

        public async Task LockAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

            var tableName = CreateTableName();
            ExecuteCommand($"CREATE TABLE {tableName} (CODE integer);");
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                RemoveLockObject(tableName);
            }
        }

        public Task LockAsync(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Task Act()
            {
                action();
                return Task.FromResult(true);
            }

            return LockAsync(Act);
        }

        public async ValueTask<LockReleaser> LockAsync()
        {
            var tableName = CreateTableName();
            ExecuteCommand($"CREATE TABLE {tableName} (CODE integer);");
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                RemoveLockObject(tableName);
            }
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