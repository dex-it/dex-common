using System;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.ClickHouse;
using NUnit.Framework;
using Octonica.ClickHouseClient;

namespace Dex.Cap.ClickHouse.Test
{
    public class Tests
    {
        private readonly ClickHouseConnectionStringBuilder _sb = new() {Host = "127.0.0.1"};

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            await using var conn = new ClickHouseConnection(_sb);
            await conn.OpenAsync();

            var oe = new OnceExecutorClickHouse<int>(conn);

            var idempotentKey = Guid.NewGuid();
            await oe.Execute(idempotentKey, (connection, token) => connection.TryPingAsync(token));
            await oe.Execute(idempotentKey, (connection, token) => throw new NotImplementedException());
        }
    }
}