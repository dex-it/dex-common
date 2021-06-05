using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Lock.Database;
using Npgsql;
using NUnit.Framework;

namespace Dex.Lock.TestProject
{
    public class DbLockTests
    {
        private const string DatabaseName = "lockdb";
        private TestDbType _databaseType = TestDbType.Postgres;

        [SetUp]
        public void Setup()
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Open();
                    connection.ChangeDatabase(DatabaseName);
                }
                catch (PostgresException e) when (e.SqlState == "3D000")
                {
                    CreateDatabase();
                }
                catch (SqlException e) when (e.Number == 911)
                {
                    CreateDatabase();
                }
            }
        }

        [TestCase(TestDbType.Postgres)]
        [TestCase(TestDbType.MsSql)]
        public async Task LockAsyncFuncTest1(TestDbType testDbType)
        {
            _databaseType = testDbType;
            const int concurrent = 10;
            const int iterations = 10;

            var targetList = new List<string>();

            async Task AddToListSample()
            {
                var r = new Random((int) DateTime.UtcNow.Ticks);
                using (var dbConnection = OpenConnection())
                using (var dbTransaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted)) // required for lock, any IsolationLevel
                {
                    var lockerProvider = new DatabaseAsyncLockProvider(dbTransaction, "6D6AC302-23");
                    var locker = lockerProvider.GetLocker("AddToListSample");

                    await using (await locker.LockAsync().ConfigureAwait(false))
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            targetList.Add(Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture));
                            await Task.Delay(r.Next(5, 10)).ConfigureAwait(false);
                        }
                    }
                }
            }

            // act
            var tasks = Enumerable.Range(0, concurrent).Select(_ => AddToListSample());
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // check
            Assert.AreEqual(concurrent * iterations, targetList.Count);
        }

        //--
        private IDbConnection CreateConnection()
        {
            return _databaseType switch
            {
                TestDbType.Postgres => new NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=my-pass~003;"),
                TestDbType.MsSql => new SqlConnection("Server=.;Database=master;User Id=sa;Password=8xM*pH@dnOz!;"),
                _ => throw new ArgumentOutOfRangeException(nameof(_databaseType), _databaseType, null)
            };
        }

        private IDbConnection OpenConnection()
        {
            var dbConnection = CreateConnection();
            dbConnection.Open();
            dbConnection.ChangeDatabase(DatabaseName);
            return dbConnection;
        }

        private void CreateDatabase()
        {
            using (var connection2 = CreateConnection())
            {
                connection2.Open();
                using (var cmd = connection2.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE {DatabaseName};";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public enum TestDbType
        {
            Postgres,
            MsSql
        }
    }
}