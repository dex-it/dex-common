using System;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.ClickHouse;
using NUnit.Framework;
using Octonica.ClickHouseClient;

namespace Dex.Cap.ClickHouse.Test
{
    public class OnceExecutorClickHouseTests
    {
        private readonly ClickHouseConnectionStringBuilder _sb = new() { Host = "127.0.0.1" };

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            await using var conn = new ClickHouseConnection(_sb);
            await conn.OpenAsync();

            IOnceExecutor<IClickHouseOptions, ClickHouseConnection> executor = new OnceExecutorClickHouse(conn);

            var idempotentKey = Guid.NewGuid().ToString("N");
            await executor.ExecuteAndSaveInTransactionAsync(idempotentKey, (connection, token) => connection.TryPingAsync(token));
            await executor.ExecuteAndSaveInTransactionAsync(idempotentKey, (_, _) => throw new NotImplementedException());
        }
    }
}