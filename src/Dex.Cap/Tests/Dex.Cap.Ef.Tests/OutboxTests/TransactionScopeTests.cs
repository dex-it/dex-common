using System;
using System.Linq;
using System.Threading.Tasks;
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
        [SetUp]
        public override Task Setup()
        {
            TestDbContext.IsRetryStrategy = true;
            return base.Setup();
        }

        [Test]
        public void AmbientAbort_CantExecuteRetryStrategy_Test1()
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
        public void AmbientAbort_NonRetryStrategy_Success_Test1()
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
        public void AmbientAbort_Supress_CommitInnerTransactionTest1()
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
        public void AmbientCommit_Requered_2Inner_Transaction_CompleteAndUncomplete_Test1()
        {
            TestDbContext.IsRetryStrategy = false;

            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Assert.Catch<TransactionAbortedException>(() =>
            {
                using (var ambient = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.RequiresNew, IsolationLevel.ReadCommitted))
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

                    db.Database.CreateExecutionStrategy()
                        .Execute(0,
                            (context, i) =>
                            {
                                using (var inner3 = TransactionScopeHelper
                                           .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                                {
                                    db.Users.Add(new TestUser { Id = id2, Name = "max2" });
                                    db.SaveChanges();

                                    //inner3.Complete();
                                }

                                return 0;
                            },
                            (context, i) => new ExecutionResult<int>(true, 0));

                    ambient.Complete();
                }
            });

            // из-за отката внутренней транзакции, внешняя транзакция откатилась данные не сохранены!

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNull(saved);

            var saved2 = db2.Users.Find(id2);
            Assert.IsNull(saved2);
        }

        [Test]
        public void AmbientCommit_Suppress_2Inner_Transaction_CompleteAndUncomplete_Test1()
        {
            TestDbContext.IsRetryStrategy = false;

            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            using (var ambient = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.Suppress, IsolationLevel.ReadCommitted))
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

                db.Database.CreateExecutionStrategy()
                    .Execute(0,
                        (context, i) =>
                        {
                            using (var inner3 = TransactionScopeHelper
                                       .CreateTransactionScope(TransactionScopeOption.Required, IsolationLevel.ReadCommitted))
                            {
                                db.Users.Add(new TestUser { Id = id2, Name = "max2" });
                                db.SaveChanges();

                                //inner3.Complete();
                            }

                            return 0;
                        },
                        (context, i) => new ExecutionResult<int>(true, 0));

                ambient.Complete();
            }

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNotNull(saved);

            var saved2 = db2.Users.Find(id2);
            Assert.IsNull(saved2);
        }

        // regular transaction

        [Test]
        public void Ambient_CommitableTransactionTest1()
        {
            TestDbContext.IsRetryStrategy = false;

            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var id = Guid.NewGuid();
            using (var aScope = new CommittableTransaction(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                Transaction.Current = aScope;

                var db = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                db.Database.CreateExecutionStrategy()
                    .Execute(0,
                        (context, i) =>
                        {
                            using (var inner2 = TransactionScopeHelper
                                       .CreateTransactionScope(TransactionScopeOption.RequiresNew, IsolationLevel.ReadCommitted))
                            {
                                db.Users.Add(new TestUser { Id = id, Name = "max" });
                                db.SaveChanges();

                                inner2.Complete();
                            }

                            return 0;
                        },
                        (context, i) => new ExecutionResult<int>(true, 0));

                aScope.Rollback();
                Transaction.Current = null;
            }

            var db2 = sp.GetRequiredService<TestDbContext>();
            var saved = db2.Users.Find(id);
            Assert.IsNotNull(saved);
        }

        [Ignore("experiments")]
        public async Task Ambient_2InnerTransactionTest()
        {
            TestDbContext.IsRetryStrategy = false;
            var sp = InitServiceCollection().BuildServiceProvider();

            using (var ambient = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                using var scope1 = sp.CreateScope();
                await using (var db1 = scope1.ServiceProvider.GetRequiredService<TestDbContext>())
                {
                    await db1.Database.BeginTransactionAsync();

                    db1.Users.Add(new TestUser { Name = "mmx1" });
                    await db1.SaveChangesAsync();

                    await db1.Database.CommitTransactionAsync();
                }

                using var scope2 = sp.CreateScope();
                await using (var db2 = scope2.ServiceProvider.GetRequiredService<TestDbContext>())
                {
                    await db2.Database.BeginTransactionAsync();

                    db2.Users.Add(new TestUser { Name = "mmx2" });
                    await db2.SaveChangesAsync();

                    await db2.Database.CommitTransactionAsync();
                }

                //ambient.Complete(); // cancell changes
            }

            using var scope3 = sp.CreateScope();
            var db3 = scope3.ServiceProvider.GetRequiredService<TestDbContext>();
            // Assert.AreEqual(2, db3.Users.Count());
            Assert.AreEqual(0, db3.Users.Count());
        }
    }
}