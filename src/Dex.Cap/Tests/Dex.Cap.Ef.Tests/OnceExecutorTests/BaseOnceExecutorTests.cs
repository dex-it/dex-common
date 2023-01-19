using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests
{
    public class BaseOnceExecutorTests : BaseTest
    {
        [Test]
        public void DoubleInsertTest1()
        {
            Assert.CatchAsync<DbUpdateException>(async () =>
            {
                var user = new TestUser { Name = "DoubleInsertTest", Years = 18 };
                await using (var testDbContext = new TestDbContext(DbName))
                {
                    await testDbContext.Users.AddAsync(user);
                    await testDbContext.SaveChangesAsync();
                }

                await using (var testDbContext = new TestDbContext(DbName))
                {
                    await testDbContext.Users.AddAsync(user);
                    await testDbContext.SaveChangesAsync();
                }
            });
        }

        [Test]
        public async Task OnceExecuteTest1()
        {
            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext>(testDbContext);

                var result = await ex.Execute(stepId,
                    (context, c) => context.Users.AddAsync(user, c).AsTask(),
                    (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", cancellationToken: c)!
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result!.Id);
            }

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext>(testDbContext);

                var result = await ex.Execute(stepId,
                    (context, c) => context.Users.AddAsync(user, c).AsTask(),
                    (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", cancellationToken: c)!
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result!.Id);
            }
        }

        [Test]
        public async Task OnceExecuteTest2()
        {
            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            await using (var testDbContext = new TestDbContext(DbName))
            {
                // without DbContext
                var ex = new OnceExecutorEf<TestDbContext>(testDbContext) as IOnceExecutor;
                await ex.Execute(stepId, _ => CreateUser(testDbContext, user).AsTask(), cancellationToken: default);
                await testDbContext.SaveChangesAsync();
            }

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var xUser = testDbContext.Find<TestUser>(user.Id);
                Assert.IsNotNull(xUser);
            }

            ValueTask<EntityEntry<TestUser>> CreateUser(TestDbContext dbContext, TestUser newUser)
            {
                return dbContext.Users.AddAsync(newUser);
            }
        }

        [Test]
        public async Task OnceExecuteBeginTransactionTest()
        {
            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteBeginTransactionTest", Years = 18 };

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext>(testDbContext);

                // transaction 1
                var result = await ex.Execute(stepId,
                    (context, c) => context.Users.AddAsync(user, c).AsTask(),
                    (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteBeginTransactionTest", cancellationToken: c)!
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result!.Id);

                await testDbContext.Users.AddAsync(new TestUser { Name = "OnceExecuteBeginTransactionTest-2" });
                // transaction 2
                await testDbContext.SaveChangesAsync();
            }

            await CheckUsers("OnceExecuteBeginTransactionTest", "OnceExecuteBeginTransactionTest-2");
        }

        private async Task CheckUsers(params string[] userNames)
        {
            await using var testDbContext = new TestDbContext(DbName);
            foreach (var u in userNames)
            {
                Assert.IsNotNull(await testDbContext.Users.SingleAsync(x => x.Name == u));
            }
        }
    }
}