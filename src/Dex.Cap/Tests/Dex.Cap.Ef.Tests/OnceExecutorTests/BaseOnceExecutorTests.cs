using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests
{
    public class BaseOnceExecutorTests : BaseTest
    {
        [Test]
        public void DoubleInsertTest1()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();

            Assert.CatchAsync<DbUpdateException>(async () =>
            {
                var user = new TestUser { Name = "DoubleInsertTest", Years = 18 };

                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();

                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();
            });
        }

        [Test]
        public async Task OnceExecuteTest1()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var executor = sp.GetRequiredService<IOnceExecutor<TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            var firstResult = await executor.ExecuteAsync(stepId,
                (context, c) => context.Users.AddAsync(user, c).AsTask(),
                (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", cancellationToken: c)
            );

            var secondResult = await executor.ExecuteAsync(stepId,
                (context, c) => context.Users.AddAsync(user, c).AsTask(),
                (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", cancellationToken: c)
            );

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(user.Id, firstResult!.Id);

            Assert.IsNotNull(secondResult);
            Assert.AreEqual(user.Id, secondResult!.Id);
        }

        [Test]
        public async Task OnceExecuteTest2()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            await executor.ExecuteAsync(stepId, (context, _) => CreateUser(context, user, default).AsTask());
            await dbContext.SaveChangesAsync();

            var xUser = dbContext.Find<TestUser>(user.Id);
            Assert.IsNotNull(xUser);

            ValueTask<EntityEntry<TestUser>> CreateUser(TestDbContext context, TestUser newUser, CancellationToken token)
            {
                return context.Users.AddAsync(newUser, token);
            }
        }

        [Test]
        public async Task OnceExecuteBeginTransactionTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteBeginTransactionTest", Years = 18 };

            // transaction 1
            var result = await executor.ExecuteAsync(stepId,
                (context, token) => context.Users.AddAsync(user, token).AsTask(),
                (context, token) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteBeginTransactionTest", token),
                IsolationLevel.ReadCommitted,
                CancellationToken.None
            );

            Assert.IsNotNull(result);
            Assert.AreEqual(user.Id, result!.Id);

            await dbContext.Users.AddAsync(new TestUser { Name = "OnceExecuteBeginTransactionTest-2" });
            // transaction 2
            await dbContext.SaveChangesAsync();

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