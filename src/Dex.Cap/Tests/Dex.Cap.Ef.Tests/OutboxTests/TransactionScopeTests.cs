using System;
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
        public void AbortAmbientTransaction_NonRetryStrategy_Success_Test1()
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

        [Test]
        public void Ambient_2Transaction_Complete_UncompleteTest1()
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
    }
}