using System;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

public class ExecuteInTransactionTests : BaseTest
{
    [Test]
    public async Task SimpleRunExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await dbContext.ExecuteInTransactionAsync(
                (dbContext, outboxService),
                async (state, token) =>
                {
                    await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);
                    await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                    await state.dbContext.SaveChangesAsync(token);
                },
                (_, _) => Task.FromResult(false));

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }
        finally
        {
            TestCommandHandler.OnProcess -= onProcess;
        }
    }

    [Test]
    public async Task RetryExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            var failureCount = 2;

            // act
            var name = "mmx_" + Guid.NewGuid();
            await dbContext.ExecuteInTransactionAsync(
                (dbContext, outboxService),
                async (state, token) =>
                {
                    await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);

                    if (failureCount-- > 0)
                    {
                        TestContext.WriteLine("throw failure...");
                        throw new TimeoutException();
                    }

                    await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                        cancellationToken: token);
                    await state.dbContext.SaveChangesAsync(token);
                }, (_, _) => Task.FromResult(false));

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }
        finally
        {
            TestCommandHandler.OnProcess -= onProcess;
        }
    }

    [Test]
    public async Task NestedExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var dbContext = sp.GetRequiredService<TestDbContext>();

        var name1 = "user1_" + Guid.NewGuid();
        var name2 = "user2_" + Guid.NewGuid();

        // act
        await dbContext.ExecuteInTransactionAsync(async token =>
        {
            await dbContext.Users.AddAsync(new TestUser { Name = name1 }, token);
            await dbContext.SaveChangesAsync(token);

            // Nested call
            await dbContext.ExecuteInTransactionAsync(async ct =>
            {
                await dbContext.Users.AddAsync(new TestUser { Name = name2 }, ct);
                await dbContext.SaveChangesAsync(ct);
            }, _ => Task.FromResult(false), cancellationToken: token);
        }, _ => Task.FromResult(false));

        // check
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name1));
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name2));
    }

    [Test]
    public async Task RetryStrategyExecuteInTransactionTest()
    {
        TestDbContext.IsRetryStrategy = true; // explicitly enable retry strategy (EnableRetryOnFailure)
        try
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
            TestCommandHandler.OnProcess += onProcess;
            try
            {
                var dbContext = sp.GetRequiredService<TestDbContext>();
                var outboxService = sp.GetRequiredService<IOutboxService>();

                var failureCount = 1;
                var name = "retry_strat_" + Guid.NewGuid();

                // act
                await dbContext.ExecuteInTransactionAsync(
                    async token =>
                    {
                        await dbContext.Users.AddAsync(new TestUser { Name = name }, token);

                        if (failureCount-- > 0)
                        {
                            TestContext.WriteLine("Simulating transient failure for RetryStrategy...");
                            // TimeoutException is usually treated as transient by EF strategies
                            throw new TimeoutException("Simulated transient error");
                        }

                        await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                        await dbContext.SaveChangesAsync(token);
                    },
                    _ => Task.FromResult(false));

                var handler = sp.GetRequiredService<IOutboxHandler>();
                await handler.ProcessAsync();

                // check
                Assert.AreEqual(1, count);
                Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            }
            finally
            {
                TestCommandHandler.OnProcess -= onProcess;
            }
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false; // restore default
        }
    }
}