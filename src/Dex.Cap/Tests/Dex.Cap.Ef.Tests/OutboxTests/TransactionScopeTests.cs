using System;
using System.Transactions;
using Dex.Cap.Common.Ef.Helpers;
using Dex.Cap.Ef.Tests.Model;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class TransactionScopeTests : BaseTest
    {
        [Test]
        public void AmbientTransaction_CantExecuteRetryStrategy_NonRetryStrategy_Test1()
        {
            TestDbContext.IsRetryStrategy = true;

            var sp = InitServiceCollection()
                .BuildServiceProvider();

            Assert.Catch<InvalidOperationException>(() =>
            {
                var id = Guid.NewGuid();
                using (var aScope = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.RequiresNew, IsolationLevel.ReadCommitted))
                {
                    var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                    db.Database.CreateExecutionStrategy()
                        .Execute(0,
                            (context, i) =>
                            {
                                using (var inner2 = TransactionScopeHelper
                                           .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                                {
                                    db.Users.Add(new TestUser { Id = id, Name = "max" });
                                    db.SaveChanges();
                                    inner2.Complete();
                                }

                                return 0;
                            },
                            (context, i) => new ExecutionResult<int>(true, 0));

                    aScope.Dispose(); // abort
                }
            });
        }

        [Test]
        public void AbortAmbientTransaction_NonRetryStrategy_Test1()
        {
            TestDbContext.IsRetryStrategy = false;

            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            using (var aScope = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.RequiresNew, IsolationLevel.ReadCommitted))
            {
                var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                db.Database.CreateExecutionStrategy()
                    .Execute(0,
                        (context, i) =>
                        {
                            using (var inner2 = TransactionScopeHelper
                                       .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                            {
                                db.Users.Add(new TestUser { Id = id, Name = "max" });
                                db.SaveChanges();
                                inner2.Complete();
                            }

                            return 0;
                        },
                        (context, i) => new ExecutionResult<int>(true, 0));

                aScope.Dispose(); // abort
            }

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNull(saved);
        }

        [Test]
        public void AbortAmbientTransaction_SupressAmbient_CommitInnerTransactionTest1()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            using (var aScope = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.RequiresNew, IsolationLevel.ReadCommitted))
            {
                using (var inner = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.Suppress, IsolationLevel.ReadCommitted))
                {
                    var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                    db.Database.CreateExecutionStrategy()
                        .Execute(0,
                            (context, i) =>
                            {
                                using (var inner2 = TransactionScopeHelper
                                           .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                                {
                                    db.Users.Add(new TestUser { Id = id, Name = "max" });
                                    db.SaveChanges();
                                    inner2.Complete();
                                }

                                return 0;
                            },
                            (context, i) => new ExecutionResult<int>(true, 0));

                    inner.Complete();
                }

                aScope.Dispose(); // abort
            }

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNotNull(saved);
        }

        [Test]
        public void Ambient_CommitableTransactionTest1()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            using (var aScope = new CommittableTransaction(new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                db.Database.CreateExecutionStrategy()
                    .Execute(0,
                        (context, i) =>
                        {
                            using (var inner2 = TransactionScopeHelper
                                       .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                            {
                                db.Users.Add(new TestUser { Id = id, Name = "max" });
                                db.SaveChanges();
                                inner2.Complete();
                            }

                            return 0;
                        },
                        (context, i) => new ExecutionResult<int>(true, 0));

                aScope.Rollback();
            }

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNotNull(saved);
        }
    }
}