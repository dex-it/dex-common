using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Dex.Lock.Async;

namespace Dex.Lock.Database
{
    public class DatabaseAsyncLockProvider : IAsyncLockProvider<string, DbLockReleaser>
    {
        private readonly IDbTransaction _dbTransaction;
        private readonly string _instanceId;
        private IDbConnection DbConnection => _dbTransaction.Connection;

        public DatabaseAsyncLockProvider(IDbTransaction dbTransaction, string? instanceId)
        {
            _dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));

            var nInstanceId = RemoveSymbols(instanceId);
            _instanceId = (nInstanceId?.Length != 0
                ? nInstanceId
                : Guid.NewGuid().ToString("N"))!;
        }

        public IAsyncLock<DbLockReleaser> GetLocker(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return new DatabaseAsyncLock(DbConnection, CreateKey(key));
        }

        private string CreateKey(string key)
        {
            var nKey = RemoveSymbols(key);

            if (string.IsNullOrEmpty(nKey))
                throw new InvalidDataException("key, must contains only letters, digits (en locale)");

            return _instanceId + nKey;
        }

        private static string? RemoveSymbols(string? instanceId)
        {
            if (instanceId == null) return null;
            return Regex.Replace(instanceId, "[^A-Za-z0-9]", "");
        }
    }
}