using System;
using System.Collections.Generic;
using System.Data;
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
            }
        }

        [Test]
        public async Task LockAsyncFuncTest1()
        {
            const int concurrent = 10;
            const int iterations = 10;

            var targetList = new List<string>();

            async Task AddToListSample()
            {
                using (var dbConnection = OpenConnection())
                using (var dbTransaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted)) // required for lock, any IsolationLevel
                {
                    var lockerProvider = new DatabaseAsyncLockProvider(dbTransaction, "6D6AC302-23");
                    var locker = lockerProvider.GetLocker("AddToListSample");

                    using (await locker.LockAsync())
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            targetList.Add(Thread.CurrentThread.ManagedThreadId.ToString());
                            await Task.Delay(10);
                        }
                    }
                }
            }

            // act
            var tasks = Enumerable.Range(0, concurrent).Select(_ => AddToListSample());
            await Task.WhenAll(tasks);

            // check
            Assert.AreEqual(concurrent * iterations, targetList.Count);
        }

        //--
        private static IDbConnection CreateConnection()
        {
            return new NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=my-pass~003;");
        }

        private static IDbConnection OpenConnection()
        {
            var dbConnection = CreateConnection();
            dbConnection.Open();
            dbConnection.ChangeDatabase(DatabaseName);
            return dbConnection;
        }
    }
}