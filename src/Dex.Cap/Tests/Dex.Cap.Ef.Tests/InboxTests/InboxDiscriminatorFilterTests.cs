using System.Linq;
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
/// Выборка фильтрует по дискриминаторам с зарегистрированным обработчиком. Это единственное, что не даёт
/// сервису забрать сообщения чужого потребителя из общей таблицы.
/// </summary>
public class InboxDiscriminatorFilterTests : BaseTest
{
    [Test]
    public async Task Process_MessageOfAnotherConsumer_IsLeftUntouched()
    {
        // Обработчик есть только у TestInboxCommand. TestErrorInboxCommand в этом сервисе не обслуживается,
        // но лежит в той же таблице.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "mine" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestErrorInboxCommand { Args = "not mine" }, new InboxMessageIdentity("m-2", "c-1"));

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        // Взято ровно одно: SQL выборки обязан отфильтровать чужой дискриминатор.
        Assert.AreEqual(1, processed);

        var envelopes = await GetDb(sp).Set<InboxEnvelope>().ToListAsync();

        var mine = envelopes.Single(x => x.MessageId == "m-1");
        Assert.AreEqual(InboxMessageStatus.Succeeded, mine.Status);

        var foreign = envelopes.Single(x => x.MessageId == "m-2");
        Assert.AreEqual(InboxMessageStatus.New, foreign.Status, "a message this service does not handle must stay untouched");
        Assert.AreEqual(0, foreign.Retries);
        Assert.IsNull(foreign.LockId, "the fetch must not even take a lease on a foreign message");
    }

    [Test]
    public async Task FreeMessagesCount_CountsOnlyWhatThisServiceCanHandle()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "mine" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestErrorInboxCommand { Args = "not mine" }, new InboxMessageIdentity("m-2", "c-1"));

        using var scope = sp.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IInboxDataProvider>();

        // Глубина очереди этого сервиса: чужое сообщение он не заберёт никогда, и включать его в свою
        // глубину означало бы навсегда залипший алерт.
        Assert.AreEqual(1, provider.GetFreeMessagesCount());
        Assert.AreEqual(0, provider.GetDeadLetteredMessagesCount());
    }
}
