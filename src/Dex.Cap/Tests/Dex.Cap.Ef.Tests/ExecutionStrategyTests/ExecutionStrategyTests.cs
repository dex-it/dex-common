using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.ExecutionStrategyTests
{
    public class ExecutionStrategyTests : BaseTest
    {
        [Test]
        public async Task ExecuteInTransactionScopeWithInnerOnceExecutorInTransactionScope_DoesNotThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "Test", Years = 18 };

            await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionScopeAsync(
                (dbContext, executor),
                async (state, ct) =>
                {
                    await state.executor.ExecuteAsync(stepId, (context, t) => context.Users.AddAsync(user, t).AsTask(), cancellationToken: ct);
                });
        }

        [Test]
        public void ExecuteInTransactionWithInnerOnceExecutorInTransactionScope_ThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "Test", Years = 18 };

            // Root ambient transaction was completed before the nested transaction. The more nested transactions should be completed first.
            Assert.CatchAsync<InvalidOperationException>(async () =>
            {
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                    (dbContext, executor),
                    async (state, ct) =>
                    {
                        await state.executor.ExecuteAsync(stepId, (context, token) => context.Users.AddAsync(user, token).AsTask(), cancellationToken: ct);
                    },
                    (state, ct) => state.dbContext.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
            });
        }

        [Test]
        public void ExecuteInTransactionWithInnerExecuteInTransactionScope_ThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();

            var user = new TestUser { Name = "Test", Years = 18 };

            // Root ambient transaction was completed before the nested transaction. The more nested transactions should be completed first.
            Assert.CatchAsync<InvalidOperationException>(async () =>
            {
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(dbContext,
                    async (context, ct) =>
                    {
                        await context.Database.CreateExecutionStrategy()
                            .ExecuteInTransactionScopeAsync(async x => // несовместимо с ExecuteInTransactionAsync
                            {
                                await context.Users.AddAsync(user, ct).AsTask();
                                await context.SaveChangesAsync(x);
                            }, cancellationToken: ct);
                    },
                    (context, ct) => context.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
            });
        }

        [Test]
        public void ExecuteInTransactionWithInnerExecuteInTransaction_ThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();

            var user = new TestUser { Name = "Test", Years = 18 };

            // The connection is already in a transaction and cannot participate in another transaction.
            Assert.CatchAsync<InvalidOperationException>(async () =>
            {
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(dbContext,
                    async (context, ct) =>
                    {
                        await context.Database.CreateExecutionStrategy()
                            .ExecuteInTransactionAsync(context, async (c, t) =>
                                {
                                    await c.Users.AddAsync(user, t).AsTask();
                                    await c.SaveChangesAsync(t);
                                },
                                (_, _) => Task.FromResult(true),
                                ct);
                    },
                    (context, ct) => context.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
            });
        }

        [Test]
        public void ExecuteInTransactionScopeWithInnerExecuteInTransaction_ThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();

            var user = new TestUser { Name = "Test", Years = 18 };

            // An ambient transaction has been detected. The ambient transaction needs to be completed before beginning a transaction on this connection.
            Assert.CatchAsync<InvalidOperationException>(async () =>
            {
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionScopeAsync(dbContext,
                    async (context, ct) =>
                    {
                        await context.Database.CreateExecutionStrategy()
                            .ExecuteInTransactionAsync(context, async (c, t) =>
                                {
                                    await c.Users.AddAsync(user, t).AsTask();
                                    await c.SaveChangesAsync(t);
                                },
                                (_, _) => Task.FromResult(true),
                                ct);
                    },
                    (context, ct) => context.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
            });
        }
    }
}