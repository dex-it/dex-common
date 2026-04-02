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
        TestCommandHandler.OnProcess += (_, _) => { count++; };

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var dbContext = sp.GetRequiredService<TestDbContext>();

        // act
        var name = "mmx_" + Guid.NewGuid();
        await dbContext.ExecuteInTransactionAsync(
            (dbContext, outboxService),
            async (state, token) =>
            {
                await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);
                await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                    cancellationToken: token);
                // SaveChangesAsync is called automatically if changes are detected, but explicit call is also fine
                await state.dbContext.SaveChangesAsync(token);
            },
            (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync();

        // check
        Assert.AreEqual(1, count);
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
    }

    [Test]
    public async Task RetryExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var count = 0;
        TestCommandHandler.OnProcess += (_, _) => { count++; };

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
}
