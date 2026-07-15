using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Фоновый обработчик как его поднимает потребитель: через AddDefaultInboxScheduler и реальный хост.
/// Остальные тесты дёргают <see cref="IInboxHandler"/> напрямую, поэтому весь слой планировщика,
/// включая регистрации чистки в DI, иначе не проверялся бы вовсе.
/// </summary>
public class InboxSchedulerTests : BaseTest
{
    private static readonly TimeSpan NoInitDelay = TimeSpan.Zero;

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

    /// <summary>
    /// Period это пауза между исчерпанными циклами, а не потолок пропускной способности. Пока обработчик
    /// забирает полные партии, он обязан продолжать без паузы.
    /// </summary>
    /// <remarks>
    /// Гарантию реализует единственная ветка `if (processed >= MessagesToProcess) continue;`. Без этого теста
    /// её потеря не покраснила бы ни один тест, а на проде обернулась бы падением пропускной способности до
    /// MessagesToProcess сообщений за Period: здесь это 2 сообщения в минуту вместо четырёх за секунды.
    /// </remarks>
    [Test]
    public async Task Scheduler_WhileBatchesComeFull_DoesNotPauseBetweenCycles()
    {
        const int messagesToProcess = 2;
        const int totalMessages = 4;

        using var host = BuildHost(messagesToProcess, period: TimeSpan.FromMinutes(1));

        await EnqueueAsync(host.Services, totalMessages);

        var processed = new CountdownEvent(totalMessages);
        TestInboxCommandHandler.OnProcessAsync = _ =>
        {
            processed.Signal();
            return Task.CompletedTask;
        };

        var elapsed = Stopwatch.StartNew();
        await host.StartAsync();

        try
        {
            // Два полных цикла подряд. При безусловной паузе после цикла хватило бы только на первую партию,
            // а остаток ждал бы Period, то есть минуту.
            Assert.IsTrue(processed.Wait(TimeSpan.FromSeconds(30)),
                $"only {totalMessages - processed.CurrentCount} of {totalMessages} messages were processed: " +
                "the handler paused between full batches instead of draining the queue");
        }
        finally
        {
            await host.StopAsync();
        }

        Assert.Less(elapsed.Elapsed, TimeSpan.FromMinutes(1), "the queue must be drained without waiting for Period");
    }

    /// <summary>
    /// Чистка резолвится из DI, а не собирается тестом вручную.
    /// </summary>
    /// <remarks>
    /// Все остальные тесты чистки конструируют провайдер напрямую, поэтому ошибка в самой регистрации
    /// (не тот тип, не то время жизни) прошла бы мимо них и всплыла бы только в проде, внутри фонового
    /// чистильщика.
    /// </remarks>
    [Test]
    public async Task Scheduler_RegistersCleanupProvider_ResolvableFromDi()
    {
        using var host = BuildHost(messagesToProcess: 2, period: TimeSpan.FromMinutes(1));

        using var scope = host.Services.CreateScope();
        var cleaner = scope.ServiceProvider.GetRequiredService<IInboxCleanupDataProvider>();

        var removed = await cleaner.Cleanup(TimeSpan.FromDays(30), CancellationToken.None);

        Assert.AreEqual(0, removed, "nothing was processed yet, so there is nothing to clean up");
    }

    private IHost BuildHost(int messagesToProcess, TimeSpan period) =>
        new HostBuilder()
            .ConfigureServices(services =>
            {
                AddLogging(services);
                services
                    .AddScoped(_ => new TestDbContext(DbName))
                    .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
                    .AddInbox<TestDbContext>(options =>
                    {
                        options.MessagesToProcess = messagesToProcess;
                        options.ConcurrencyLimit = 1;
                    })
                    .AddDefaultInboxScheduler<TestDbContext>(options =>
                    {
                        options.Period = period;

                        // Init-delay разводит старты реплик, а здесь реплика одна и её ожидание было бы
                        // чистым простоем теста.
                        options.HandlerInitDelay = new InitDelayRange { Min = NoInitDelay, Max = NoInitDelay };
                        options.CleanerInitDelay = new InitDelayRange { Min = NoInitDelay, Max = NoInitDelay };
                    });
            })
            .Build();

    private static async Task EnqueueAsync(IServiceProvider serviceProvider, int count)
    {
        using var scope = serviceProvider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxService>();

        for (var i = 0; i < count; i++)
        {
            await inbox.EnqueueAsync(
                new TestInboxCommand { Args = $"message-{i}" },
                new InboxMessageIdentity($"message-{i}", "consumer-1"));
        }
    }
}
