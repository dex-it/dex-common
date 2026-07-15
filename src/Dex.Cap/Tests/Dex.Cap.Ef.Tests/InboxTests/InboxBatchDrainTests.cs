using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Аренда всей захваченной партии тикает с момента захвата, а джобы идут по ConcurrencyLimit за раз.
/// Сообщение, чья аренда умерла в очереди на обработку, обработчик не видел, поэтому попытку тратить нельзя.
/// </summary>
public class InboxBatchDrainTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestInboxCommandHandler.OnProcessAsync = null;
    }

    [TearDown]
    public void ResetHook()
    {
        TestInboxCommandHandler.OnProcessAsync = null;
    }

    [Test]
    public async Task Process_LeaseExpiredWhileWaitingInQueue_AttemptIsNotSpent()
    {
        var sp = InitInboxServiceCollection(messageToProcessLimit: 2, parallelLimit: 1)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();

        // Минимально допустимая аренда 10s даёт токену 5s запаса. Первый обработчик держит семафор дольше,
        // поэтому второе сообщение дожидается очереди уже с истёкшей арендой.
        await inbox.EnqueueAsync(
            new TestInboxCommand { Args = "slow" },
            new InboxMessageIdentity("message-1", "consumer-1"),
            lockTimeout: TimeSpan.FromSeconds(10));

        await inbox.EnqueueAsync(
            new TestInboxCommand { Args = "starved" },
            new InboxMessageIdentity("message-2", "consumer-1"),
            lockTimeout: TimeSpan.FromSeconds(10));

        var processedCount = 0;
        TestInboxCommandHandler.OnProcessAsync = async _ =>
        {
            if (Interlocked.Increment(ref processedCount) == 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(6));
            }
        };

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        var db = GetDb(sp);
        var starved = await db.Set<InboxEnvelope>().SingleAsync(x => x.MessageId == "message-2");

        // Главное: попытка не потрачена. Иначе несколько таких циклов похоронили бы сообщение,
        // которого обработчик ни разу не видел.
        Assert.AreEqual(0, starved.Retries, "a message the handler never saw must not lose an attempt");
        Assert.AreNotEqual(InboxMessageStatus.DeadLettered, starved.Status);
        Assert.IsNotNull(starved.ScheduledStartIndexing, "the message must stay in the fetch set");
    }
}
