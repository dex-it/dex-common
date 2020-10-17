using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dex.Lock.Async;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLock : IAsyncLock
    {
        private readonly IDbConnection _dataConnection;
        private readonly string _key;

        internal DatabaseAsyncLock([NotNull] IDbConnection dataConnection, [NotNull] string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _dataConnection = dataConnection ?? throw new ArgumentNullException(nameof(dataConnection));
            _key = key.ToUpper();
        }

        public async Task LockAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

            var tableName = CreateTableName();
            ExecuteCommand($"CREATE TABLE {tableName} (CODE integer);");
            try
            {
                await asyncAction();
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

        internal void RemoveLockObject(string tableName)
        {
            ExecuteCommand($"DROP TABLE {tableName};");
        }

        private string CreateTableName()
        {
            return $"LT_{_key}";
        }

        private void ExecuteCommand([NotNull] string command)
        {
            using var cmd = _dataConnection.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }
    }
}