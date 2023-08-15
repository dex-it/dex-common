using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Interfaces;
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
            var executor = sp.GetRequiredService<IOnceExecutor<IEfOptions, TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "Test", Years = 18 };

            await dbContext.ExecuteInTransactionScopeAsync(
                (dbContext, executor),
                async (state, ct) =>
                {
                    await state.executor.ExecuteAsync(stepId, async (context, t) =>
                    {
                        await context.Users.AddAsync(user, t);
                        await state.dbContext.SaveChangesAsync(t);
                    }, cancellationToken: ct);

                    await dbContext.SaveChangesAsync(ct);
                },
                (_, _) => Task.FromResult(false));
        }

        [Test]
        public async Task OutboxExecuteOperationInOutboxExecuteOperation_DoesNotThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            var correlationId = Guid.NewGuid();
            var name = "mmx_" + Guid.NewGuid();
            var anotherName = "mmx_" + Guid.NewGuid();

            // act
            await outboxService.ExecuteOperationAsync(
                correlationId,
                async (token, outboxContext) =>
                {
                    await outboxContext.DbContext.Users.AddAsync(new TestUser { Name = name }, token);
                    // обязательно перед вызовом следующего этапа процесса - сохранить данные
                    await outboxContext.DbContext.SaveChangesAsync(token);

                    await outboxService.ExecuteOperationAsync(correlationId,
                        async (t, context) => { await context.DbContext.Users.AddAsync(new TestUser { Name = anotherName }, t); }, token);
                }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.IsTrue(await outboxService.IsOperationExistsAsync(correlationId, CancellationToken.None));
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == anotherName));
        }

        [Test]
        public void ExecuteInTransactionWithInnerOnceExecutorInTransactionScope_ThrowException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<IEfOptions, TestDbContext>>();

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
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                    dbContext,
                    async (context, ct) =>
                    {
                        await context.ExecuteInTransactionScopeAsync(
                            async t => // несовместимо с ExecuteInTransactionAsync
                            {
                                await context.Users.AddAsync(user, t).AsTask();
                                await context.SaveChangesAsync(t);
                            },
                            _ => Task.FromResult(false),
                            cancellationToken: ct);
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
                await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                    dbContext,
                    async (context, ct) =>
                    {
                        await context.Database.CreateExecutionStrategy()
                            .ExecuteInTransactionAsync(context, async (c, t) =>
                                {
                                    await c.Users.AddAsync(user, t).AsTask();
                                    await c.SaveChangesAsync(t);
                                },
                                (_, _) => Task.FromResult(false),
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
                await dbContext.ExecuteInTransactionScopeAsync(
                    dbContext,
                    async (context, ct) =>
                    {
                        await context.Database.CreateExecutionStrategy()
                            .ExecuteInTransactionAsync(context, async (c, t) =>
                                {
                                    await c.Users.AddAsync(user, t).AsTask();
                                    await c.SaveChangesAsync(t);
                                },
                                (_, _) => Task.FromResult(false),
                                ct);
                    },
                    (context, ct) => context.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
            });
        }
    }
}