using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests
{
    public class OnceExecutorTests : BaseTest
    {
        [Test]
        public void DoubleInsertTest()
        {
            Assert.CatchAsync<DbUpdateException>(async () =>
            {
                var user = new User {Name = "DoubleInsertTest", Years = 18};
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
        public async Task OnceExecuteTest()
        {
            var stepId = Guid.NewGuid();
            var user = new User {Name = "OnceExecuteTest", Years = 18};

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext, User>(testDbContext);

                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest")
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result.Id);
            }

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext, User>(testDbContext);

                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest")
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result.Id);
            }
        }

        [Test]
        public async Task OnceExecuteIntoAmbientTransactionTest()
        {
            var stepId = Guid.NewGuid();
            var user = new User {Name = "OnceExecuteIntoAmbientTransactionTest", Years = 18};

            await using (var testDbContext = new TestDbContext(DbName))
            await using (var t = await testDbContext.Database.BeginTransactionAsync()) // ambient transaction
            {
                var ex = new OnceExecutorEf<TestDbContext, User>(testDbContext);

                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteIntoAmbientTransactionTest")
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result.Id);

                await testDbContext.Users.AddAsync(new User() {Name = "OnceExecuteIntoAmbientTransactionTest-2"});
                await testDbContext.SaveChangesAsync();

                await t.CommitAsync();
            }

            await CheckUsers("OnceExecuteIntoAmbientTransactionTest", "OnceExecuteIntoAmbientTransactionTest-2");
        }

        [Test]
        public async Task OnceExecuteBeginTransactionTest()
        {
            var stepId = Guid.NewGuid();
            var user = new User {Name = "OnceExecuteBeginTransactionTest", Years = 18};

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEf<TestDbContext, User>(testDbContext);

                // transaction 1
                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteBeginTransactionTest")
                );

                Assert.IsNotNull(result);
                Assert.AreEqual(user.Id, result.Id);

                await testDbContext.Users.AddAsync(new User() {Name = "OnceExecuteBeginTransactionTest-2"});
                // transaction 2
                await testDbContext.SaveChangesAsync();
            }

            await CheckUsers("OnceExecuteBeginTransactionTest", "OnceExecuteBeginTransactionTest-2");
        }

        private async Task CheckUsers(params string[] userNames)
        {
            await using (var testDbContext = new TestDbContext(DbName))
            {
                foreach (var u in userNames)
                {
                    Assert.IsNotNull(await testDbContext.Users.SingleAsync(x => x.Name == u));
                }
            }
        }
    }
}