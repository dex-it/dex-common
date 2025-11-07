using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

public class ExecuteTransactionOutboxTests : BaseTest
{
    [Test]
    public async Task IdempotentRetryExecutionContextTest1()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, IdempotentCreateUserCommandHandler>()
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var dbContext = sp.GetRequiredService<TestDbContext>();
        var handler = sp.GetRequiredService<IOutboxHandler>();

        // act
        var name = "mmx_" + Guid.NewGuid();
        await dbContext.ExecuteInTransactionScopeAsync(
            (dbContext, outboxService),
            async (state, token) =>
            {
                await state.outboxService.EnqueueAsync(new TestUserCreatorCommand { UserName = name },
                    cToken: token);
                await state.dbContext.SaveChangesAsync(token);
            },
            (_, _) => Task.FromResult(false));

        dbContext.ChangeTracker.Clear();
        await handler.ProcessAsync(CancellationToken.None);

        // repeat
        dbContext.ChangeTracker.Clear();
        var envelope = await dbContext.Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
        envelope.Status = OutboxMessageStatus.New;
        envelope.ScheduledStartIndexing = envelope.StartAtUtc;
        await SaveChanges(sp);

        dbContext.ChangeTracker.Clear();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        dbContext.ChangeTracker.Clear();
        envelope = await dbContext.Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
        Assert.AreEqual(OutboxMessageStatus.Succeeded, envelope.Status);
    }

    [Test]
    public async Task NonIdempotentRetryExecutionContextTest1()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, NonIdempotentCreateUserCommandHandler>()
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var dbContext = sp.GetRequiredService<TestDbContext>();
        var handler = sp.GetRequiredService<IOutboxHandler>();

        // act
        var name = "mmx_" + Guid.NewGuid();
        await dbContext.ExecuteInTransactionScopeAsync(
            (dbContext, outboxService),
            async (state, token) =>
            {
                await state.outboxService.EnqueueAsync(new TestUserCreatorCommand { UserName = name },
                    cToken: token);
                await state.dbContext.SaveChangesAsync(token);
            },
            (_, _) => Task.FromResult(false));

        dbContext.ChangeTracker.Clear();
        await handler.ProcessAsync(CancellationToken.None);

        // repeat
        dbContext.ChangeTracker.Clear();
        var envelope = await dbContext.Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
        envelope.Status = OutboxMessageStatus.New;
        envelope.ScheduledStartIndexing = envelope.StartAtUtc;
        await SaveChanges(sp);

        dbContext.ChangeTracker.Clear();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        dbContext.ChangeTracker.Clear();
        envelope = await dbContext.Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
        Assert.AreEqual(OutboxMessageStatus.Failed, envelope.Status);
    }

    [Test]
    public async Task TransactionalExecutionContextTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, TransactionalCreateUserCommandHandler>()
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var dbContext = sp.GetRequiredService<TestDbContext>();

        // act
        var name = "mmx_" + Guid.NewGuid();
        await outboxService.EnqueueAsync(new TestUserCreatorCommand { UserName = name });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        using var scope = sp.CreateScope();
        var envelope = await GetDb(scope.ServiceProvider)
            .Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
        Assert.AreEqual(OutboxMessageStatus.Succeeded, envelope.Status);
    }

    [Test]
    public async Task SimpleRunExecuteInTransactionWithoutContextTest1()
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
        await dbContext.ExecuteInTransactionScopeAsync(
            (dbContext, outboxService),
            async (state, token) =>
            {
                await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);
                await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                    cToken: token);
                await state.dbContext.SaveChangesAsync(token);
            },
            (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        Assert.AreEqual(1, count);
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
    }

    [Test]
    public async Task SimpleRunExecuteInTransactionWithContextTest2()
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
        await dbContext.ExecuteInTransactionScopeAsync(
            new { Name = name, Age = 25, DbContext = dbContext, OutboxService = outboxService },
            async (state, token) =>
            {
                var entity = new TestUser
                {
                    Name = state.Name,
                    Years = state.Age
                };

                await state.DbContext.Users.AddAsync(entity, token);
                await state.OutboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                    cToken: token);
                await state.DbContext.SaveChangesAsync(token);
            }, (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        Assert.AreEqual(1, count);
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
    }

    [Test]
    public async Task SeveralOutboxMessagesInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand2>, TestCommand2Handler>()
            .BuildServiceProvider();

        var count = 0;
        TestCommandHandler.OnProcess += (_, _) => { count++; };
        TestCommand2Handler.OnProcess += (_, _) => { count++; };

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var dbContext = sp.GetRequiredService<TestDbContext>();
        var logger = sp.GetService<ILogger<EnqueueOutboxTests>>();

        // act
        var name = "mmx_" + Guid.NewGuid();
        await dbContext.ExecuteInTransactionScopeAsync(
            (logger, outboxService, dbContext),
            async (state, token) =>
            {
                state.logger?.LogDebug("DEBUG...");

                await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);
                await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                    cToken: token);
                await state.outboxService.EnqueueAsync(new TestOutboxCommand2 { Args = "Command2" },
                    cToken: token);
                await state.dbContext.SaveChangesAsync(token);
            }, (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        Assert.AreEqual(2, count);
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
        await dbContext.ExecuteInTransactionScopeAsync(
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
                    cToken: token);
                await state.dbContext.SaveChangesAsync(token);
            }, (_, _) => Task.FromResult(false));

        var handler = sp.GetRequiredService<IOutboxHandler>();
        await handler.ProcessAsync(CancellationToken.None);

        // check
        Assert.AreEqual(1, count);
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
    }

    [Test]
    public async Task ParallelProcessingMessagesTest()
    {
        // set process limit to 1
        var services = InitServiceCollection(1)
            .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
            .BuildServiceProvider();

        var threads = new ConcurrentBag<string>();
        using var ce = new CountdownEvent(3);

        TestDelayCommandHandler.OnProcess += (_, c) =>
        {
            threads.Add(c.Args);
            // ReSharper disable once AccessToDisposedClosure
            ce.Signal();
        };

        var num = 0;

        var outboxService = services.GetRequiredService<IOutboxService>();
        await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
        await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
        await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
        await SaveChanges(services);

        var sw = Stopwatch.StartNew();
        ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());
        ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());
        ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());

        // wait for countdown
        ce.Wait();
        sw.Stop();

        Assert.AreEqual(num, threads.Distinct().Count());
        Assert.LessOrEqual((int)sw.Elapsed.TotalSeconds, 1);
        return;

        async void RunOutboxHandler()
        {
            using var scope = services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();
        }

        string GetId() => (num++).ToString("D");
    }
}