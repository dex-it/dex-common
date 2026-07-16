using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Ef;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Выборка фильтрует по дискриминаторам с зарегистрированным обработчиком. Это единственное, что не даёт
/// сервису забрать сообщения чужого потребителя из общей таблицы.
/// </summary>
public class InboxDiscriminatorFilterTests : BaseTest
{
    [Test]
    public async Task Process_MessageWithNoHandlerInThisService_IsLeftUntouched()
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
    }

    [Test]
    public async Task DeadLetteredCount_CountsOnlyWhatThisServiceCanHandle()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "mine" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestErrorInboxCommand { Args = "not mine" }, new InboxMessageIdentity("m-2", "c-1"));

        // Хороним обе строки: свою и чужую. Разбирать чужую не этому сервису.
        using (var buryScope = sp.CreateScope())
        {
            await buryScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.DeadLettered)
                    .SetProperty(x => x.ScheduledStartIndexing, (DateTime?)null));
        }

        using var scope = sp.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IInboxDataProvider>();

        // Ровно та же логика охвата, что и у глубины очереди: иначе алерт на разбор похороненных
        // залипнет на чужих сообщениях.
        Assert.AreEqual(1, provider.GetDeadLetteredMessagesCount());
        Assert.AreEqual(0, provider.GetFreeMessagesCount(), "buried messages leave the queue depth");
    }

    /// <summary>
    /// Сервис без единого обработчика не владеет ни одним сообщением, поэтому не трогает ничего.
    /// </summary>
    /// <remarks>
    /// Вырожденный случай того же фильтра, и цена ошибки в нём наибольшая: пустой набор дискриминаторов,
    /// подставленный в запрос вместо раннего выхода, снял бы фильтр в чистке и стёр бы чужие строки, то есть
    /// молча укоротил бы окно дедупликации соседа. Штатно так выглядит сервис, у которого убрали последний
    /// обработчик, оставив приём.
    /// </remarks>
    [Test]
    public async Task ServiceWithoutHandlers_OwnsNothing_AndTouchesNothing()
    {
        var owner = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = owner.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "waiting" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "old" }, new InboxMessageIdentity("m-2", "c-1"));

        // Вторая строка обработана и достаточно стара, чтобы попасть под чистку.
        using (var ageScope = owner.CreateScope())
        {
            await ageScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .Where(x => x.MessageId == "m-2")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.Succeeded)
                    .SetProperty(x => x.CreatedUtc, DateTime.UtcNow.AddDays(-60))
                    .SetProperty(x => x.ScheduledStartIndexing, (DateTime?)null));
        }

        // Та же таблица, но обработчиков не зарегистрировано вовсе.
        var handlerless = InitInboxServiceCollection().BuildServiceProvider();

        using var scope = handlerless.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IInboxDataProvider>();

        Assert.AreEqual(0, provider.GetFreeMessagesCount());
        Assert.AreEqual(0, provider.GetDeadLetteredMessagesCount());
        Assert.AreEqual(0, await handlerless.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None));

        var cleaner = new InboxCleanupDataProviderEf<TestDbContext>(
            scope.ServiceProvider.GetRequiredService<TestDbContext>(),
            scope.ServiceProvider.GetRequiredService<IInboxTypeDiscriminatorProvider>(),
            Options.Create(new InboxHandlerOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanupDataProviderEf<TestDbContext>>>());

        Assert.AreEqual(0, await cleaner.Cleanup(TimeSpan.FromDays(30), CancellationToken.None));

        Assert.AreEqual(2, await GetDb(owner).Set<InboxEnvelope>().CountAsync(),
            "a service that owns no discriminators must not delete rows that belong to someone else");
    }
}
