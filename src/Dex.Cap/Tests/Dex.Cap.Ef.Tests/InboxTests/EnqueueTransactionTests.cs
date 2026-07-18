using System;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Приём обязан фиксировать сообщение своей транзакцией. Внутри чужой транзакции гарантии нет:
/// на откате сообщение исчезнет, хотя источнику уже отправлен ack.
/// </summary>
public class EnqueueTransactionTests : BaseTest
{
    [Test]
    public async Task Enqueue_InsideDbContextTransaction_IsRejected()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxService>();

        await using var transaction = await db.Database.BeginTransactionAsync();

        var ex = NUnit.Framework.Assert.ThrowsAsync<InboxException>((Func<Task>)(async () =>
            await inbox.EnqueueAsync(
                new TestInboxCommand { Args = "in-transaction" },
                new InboxMessageIdentity("message-1", "consumer-1"))));

        Assert.IsTrue(ex!.Message.Contains("enclosing DbContext transaction", StringComparison.Ordinal));

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Enqueue_InsideAmbientTransactionScope_IsRejected()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        using var scope = sp.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxService>();

        using var transactionScope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        var ex = NUnit.Framework.Assert.ThrowsAsync<InboxException>((Func<Task>)(async () =>
            await inbox.EnqueueAsync(
                new TestInboxCommand { Args = "in-scope" },
                new InboxMessageIdentity("message-2", "consumer-1"))));

        Assert.IsTrue(ex!.Message.Contains("ambient TransactionScope", StringComparison.Ordinal));
    }

    [Test]
    public async Task Enqueue_OutsideTransaction_IsPersistedImmediately()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var status = await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "plain" },
            new InboxMessageIdentity("message-3", "consumer-1"));

        Assert.AreEqual(InboxEnqueueStatus.Accepted, status);

        // Видно из другого DbContext сразу после возврата: строка закоммичена, а не ждёт чужого коммита.
        using var otherScope = sp.CreateScope();
        var otherDb = otherScope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.AreEqual(1, await otherDb.Set<InboxEnvelope>().CountAsync());
    }
}