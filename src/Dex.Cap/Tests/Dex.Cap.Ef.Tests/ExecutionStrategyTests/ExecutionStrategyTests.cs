using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.ExecutionStrategyTests;

public class ExecutionStrategyTests : BaseTest
{
    [Test]
    public async Task ExecuteInTransactionScopeWithInnerOnceExecutorInTransactionScope_DoesNotThrowException()
    {
        var sp = InitServiceCollection()
            .BuildServiceProvider();

        var dbContext = sp.GetRequiredService<TestDbContext>();
        var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

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

        var dbContext = sp.GetRequiredService<TestDbContext>();
        var name = "mmx_name_" + Guid.NewGuid();
        var anotherName = "mmx_anotherName_" + Guid.NewGuid();
        var failureCount = 2;

        // act
        await dbContext.ExecuteInTransactionScopeAsync(
            dbContext,
            async (context, token) =>
            {
                await context.Users.AddAsync(new TestUser { Name = name }, token);
                // обязательно перед вызовом следующего этапа процесса - сохранить данные
                await dbContext.SaveChangesAsync(token);

                await context.ExecuteInTransactionScopeAsync(
                    context,
                    async (dbContextInner, ct) =>
                    {
                        await dbContextInner.Users.AddAsync(new TestUser { Name = anotherName }, ct);

                        if (failureCount-- > 0)
                        {
                            TestContext.WriteLine("throw failure...");
                            throw new TimeoutException();
                        }

                        await dbContextInner.SaveChangesAsync(ct);
                    },
                    (_, _) => Task.FromResult(false),
                    cancellationToken: token);
            },
            (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == anotherName));
    }

    [Test]
    public void ExecuteInTransactionWithInnerOnceExecutorInTransactionScope_ThrowException()
    {
        var sp = InitServiceCollection()
            .BuildServiceProvider();

        var dbContext = sp.GetRequiredService<TestDbContext>();
        var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

        var stepId = Guid.NewGuid().ToString("N");
        var user = new TestUser { Name = "Test", Years = 18 };

        // Root ambient transaction was completed before the nested transaction. The more nested transactions should be completed first.
        NUnit.Framework.Assert.CatchAsync<InvalidOperationException>(async () =>
        {
            await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                (dbContext, executor),
                async (state, ct) =>
                {
                    await state.executor.ExecuteAsync(stepId,
                        (context, token) => context.Users.AddAsync(user, token).AsTask(), cancellationToken: ct);
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
        NUnit.Framework.Assert.CatchAsync<InvalidOperationException>(async () =>
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
        NUnit.Framework.Assert.CatchAsync<InvalidOperationException>(async () =>
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
        NUnit.Framework.Assert.CatchAsync<InvalidOperationException>(async () =>
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

    [Test]
    public void ExecuteInTransactionScope_WithInnerExecuteInTransactionScopeWithSuppress_ThrowException()
    {
        var sp = InitServiceCollection()
            .BuildServiceProvider();

        var dbContext = sp.GetRequiredService<TestDbContext>();

        var user = new TestUser { Name = "Test", Years = 18 };

        //This connection was used with an ambient transaction. The original ambient transaction needs to be completed before this connection can be used outside of it.
        NUnit.Framework.Assert.CatchAsync<InvalidOperationException>(async () =>
        {
            await dbContext.ExecuteInTransactionScopeAsync(
                dbContext,
                async (context, ct) =>
                {
                    //await context.Users.FirstOrDefaultAsync(u => u.Name == "Test", ct);
                    await context.Users.AddAsync(user, ct);
                    await context.SaveChangesAsync(ct);

                    await context.ExecuteInTransactionScopeAsync
                    (context, async (c, t) => { await c.Users.AnyAsync(u => u.Name == "user", t); },
                        (_, _) => Task.FromResult(false),
                        EfTransactionOptions.DefaultSuppress,
                        cancellationToken: ct);
                },
                (context, ct) => context.Users.AnyAsync(x => x.Name == "Test", cancellationToken: ct));
        });
    }
}