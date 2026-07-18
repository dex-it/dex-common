using System;
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
/// Строка с непригодным LockTimeout не должна останавливать инбокс. Такое значение может появиться только
/// внеполосной записью: конструктор конверта проверяет минимум, у колонки есть дефолт.
/// </summary>
public class InboxPoisonedRowTests : BaseTest
{
    [Test]
    public async Task Process_RowWithLockTimeoutBelowMinimum_IsDeadLetteredAndTheBatchSurvives()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "good-1" }, new InboxMessageIdentity("good-1", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "good-2" }, new InboxMessageIdentity("good-2", "c-1"));

        // Внеполосная запись в обход конструктора: ровно так строка и становится непригодной.
        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .Where(x => x.MessageId == "poison")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        // Не бросает: одна непригодная строка не имеет права ронять сборку партии.
        // Возврат это число ЗАХВАЧЕННЫХ строк (3): два здоровых плюс одна похороненная непригодная.
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(3, processed, "the claim of three rows must be reported in full, poisoned row included");

        var envelopes = await GetDb(sp).Set<InboxEnvelope>().ToListAsync();

        foreach (var messageId in new[] { "good-1", "good-2" })
        {
            Assert.AreEqual(InboxMessageStatus.Succeeded, envelopes.Single(x => x.MessageId == messageId).Status,
                $"{messageId} must be handled despite the poisoned row");
        }

        // Непригодная строка похоронена: сама она не починится, и таскать её из цикла в цикл бессмысленно.
        var poisoned = envelopes.Single(x => x.MessageId == "poison");
        Assert.AreEqual(InboxMessageStatus.DeadLettered, poisoned.Status);
        Assert.IsNull(poisoned.ScheduledStartIndexing, "the poisoned row must leave the fetch set");
        Assert.IsNull(poisoned.LockId, "the lease must be released");
        Assert.IsTrue(poisoned.ErrorMessage!.Contains("LockTimeout", StringComparison.Ordinal));
    }

    [Test]
    public async Task Process_PoisonedRow_DoesNotStallTheInboxOnTheNextCycle()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));

        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        var handler = sp.GetRequiredService<IInboxHandler>();
        // Одна непригодная строка захвачена и похоронена: захват был, поэтому возврат единица, а не ноль.
        Assert.AreEqual(1, await handler.ProcessAsync(CancellationToken.None));

        // Ключевое: строка похоронена, поэтому следующий цикл её не перезахватывает и не падает снова.
        Assert.AreEqual(0, await handler.ProcessAsync(CancellationToken.None));

        // Новое сообщение обрабатывается как ни в чём не бывало.
        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "fresh" }, new InboxMessageIdentity("fresh", "c-1"));

        Assert.AreEqual(1, await handler.ProcessAsync(CancellationToken.None), "the inbox must keep working");
        Assert.AreEqual(InboxMessageStatus.Succeeded,
            (await GetDb(sp).Set<InboxEnvelope>().SingleAsync(x => x.MessageId == "fresh")).Status);
    }

    /// <summary>
    /// Непригодная строка в ПОЛНОЙ партии не должна выглядеть как неполная партия.
    /// </summary>
    /// <remarks>
    /// Планировщик паузит только при неполном захвате. Если бы возврат считал обработанные строки, а не
    /// захваченные, одна похороненная непригодная строка занизила бы полную партию до неполной, и обработчик
    /// ушёл бы в паузу на Period, хотя очередь не исчерпана.
    /// </remarks>
    [Test]
    public async Task Process_FullBatchWithOnePoisonedRow_ReportsFullClaim()
    {
        const int batch = 3;

        var sp = InitInboxServiceCollection(messageToProcessLimit: batch)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "a" }, new InboxMessageIdentity("a", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "poison" }, new InboxMessageIdentity("poison", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "b" }, new InboxMessageIdentity("b", "c-1"));

        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .Where(x => x.MessageId == "poison")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromSeconds(1)));
        }

        var claimed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(batch, claimed, "a full claim with one poisoned row must still report a full batch");
    }

    /// <summary>
    /// Аренда сверх максимума так же непригодна, как и аренда ниже минимума.
    /// </summary>
    /// <remarks>
    /// Таймер отмены не принимает интервал длиннее <see cref="int.MaxValue"/> миллисекунд и бросает, поэтому
    /// без верхней границы такая строка роняла бы сборку ВСЕЙ захваченной партии, унося с собой здоровые
    /// сообщения. Публичный конструктор такое значение отвергает, но из БД конверт приезжает мимо него.
    /// </remarks>
    [Test]
    public async Task Process_RowWithLockTimeoutAboveMaximum_IsDeadLetteredAndTheBatchSurvives()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "healthy" }, new InboxMessageIdentity("healthy", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "giant" }, new InboxMessageIdentity("giant", "c-1"));

        using (var poisonScope = sp.CreateScope())
        {
            await poisonScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .Where(x => x.MessageId == "giant")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockTimeout, TimeSpan.FromDays(60)));
        }

        // Возврат это число захваченных строк (2): здоровая плюс похороненная негодная с огромной арендой.
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(2, processed, "the claim of two rows must be reported in full, oversized-lease row included");

        var envelopes = await GetDb(sp).Set<InboxEnvelope>().ToListAsync();

        Assert.AreEqual(InboxMessageStatus.Succeeded, envelopes.Single(x => x.MessageId == "healthy").Status);

        var giant = envelopes.Single(x => x.MessageId == "giant");
        Assert.AreEqual(InboxMessageStatus.DeadLettered, giant.Status);
        Assert.IsNull(giant.ScheduledStartIndexing);
        Assert.IsTrue(giant.ErrorMessage!.Contains("LockTimeout", StringComparison.Ordinal));
    }

    /// <summary>
    /// Приём не принимает аренду сверх максимума: ошибка обязана прилететь вызывающему сразу, а не позже и
    /// не фоновому обработчику.
    /// </summary>
    [Test]
    public void Enqueue_LockTimeoutAboveMaximum_IsRejectedAtTheCallSite()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();

        NUnit.Framework.Assert.ThrowsAsync<ArgumentOutOfRangeException>((Func<Task>)(async () =>
            await inbox.EnqueueAsync(
                new TestInboxCommand { Args = "giant" },
                new InboxMessageIdentity("giant", "c-1"),
                lockTimeout: TimeSpan.FromDays(60))));
    }
}