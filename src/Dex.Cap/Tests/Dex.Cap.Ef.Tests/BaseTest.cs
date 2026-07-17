using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.RetryStrategies;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Options;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests;

public abstract class BaseTest
{
    protected string DbName { get; } = "db_test_" + DateTime.Now.Ticks;

    [SetUp]
    public virtual async Task Setup()
    {
        // Статические хуки общего тест-обработчика чистятся здесь, до любого теста, чтобы изоляция не
        // держалась на TearDown соседней фикстуры: базовый SetUp выполняется раньше SetUp наследника,
        // поэтому фикстуры, которым хук нужен, всё равно ставят его уже после сброса.
        TestInboxCommandHandler.Reset();

        var db = new TestDbContext(DbName);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        // await db.Database.MigrateAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        var db = new TestDbContext(DbName);
        await db.Database.EnsureDeletedAsync();
    }

    protected IServiceCollection InitServiceCollection(int messageToProcessLimit = 10, int parallelLimit = 2,
        Action<OutboxRetryStrategyConfigurator>? strategyConfigure = null)
    {
        var serviceCollection = new ServiceCollection();
        AddLogging(serviceCollection);

        serviceCollection
            .AddScoped(_ => new TestDbContext(DbName))
            .AddOutbox<TestDbContext>((_, configurator) => { strategyConfigure?.Invoke(configurator); })
            .AddDefaultOutboxScheduler<TestDbContext>(periodSeconds: 1)
            .AddOnceExecutor<TestDbContext>()
            .AddOptions<OutboxOptions>()
            .Configure(options =>
            {
                options.MessagesToProcess = messageToProcessLimit;
                options.ConcurrencyLimit = parallelLimit;
            });

        return serviceCollection;
    }

    /// <summary>
    /// Сервисы для тестов инбокса. Отдельно от <see cref="InitServiceCollection"/>, чтобы тесты Outbox
    /// не тащили за собой реестр сообщений инбокса и наоборот.
    /// </summary>
    protected IServiceCollection InitInboxServiceCollection(
        int messageToProcessLimit = 10,
        int parallelLimit = 2,
        int retries = 3,
        Action<InboxRetryStrategyConfigurator>? strategyConfigure = null)
    {
        var serviceCollection = new ServiceCollection();
        AddLogging(serviceCollection);

        serviceCollection
            .AddScoped(_ => new TestDbContext(DbName))
            .AddInbox<TestDbContext>(
                options =>
                {
                    options.MessagesToProcess = messageToProcessLimit;
                    options.ConcurrencyLimit = parallelLimit;
                    options.Retries = retries;
                },
                (_, configurator) => strategyConfigure?.Invoke(configurator));

        UseSingleAssemblyInboxTypeSource(serviceCollection);

        return serviceCollection;
    }

    /// <summary>
    /// Ограничить дискавери типов сообщений инбокса одной тест-сборкой.
    /// </summary>
    /// <remarks>
    /// Реальный <c>AppDomainInboxMessageTypeSource</c> сканирует все контексты загрузки процесса. Тест-раннер
    /// Rider (NUnit engine) держит тест-сборку в двух AssemblyLoadContext, один тип приходит дважды разными
    /// Type с общим AssemblyQualifiedName, и построение реестра падает ложным AmbiguousMessageTypeException.
    /// Скан одной сборки берёт типы одного контекста — дубля нет. Вызывать ПОСЛЕ <c>AddInbox</c>: источник
    /// регистрируется обычным Add, побеждает последняя регистрация.
    /// <para>
    /// Own-host тесты (свой HostBuilder + AddInbox, доходящие до StartAsync/Enqueue) обязаны звать это сами:
    /// через <see cref="InitInboxServiceCollection"/> они не идут, и подмена до них иначе не доедет.
    /// </para>
    /// </remarks>
    protected static void UseSingleAssemblyInboxTypeSource(IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IInboxMessageTypeSource>();
        serviceCollection.AddSingleton<IInboxMessageTypeSource>(
            new SingleAssemblyInboxMessageTypeSource(typeof(BaseTest).Assembly));
    }

    protected static void AddLogging(IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddProvider(new TestLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Trace);
        });
    }

    protected static TestDbContext GetDb(IServiceProvider sp)
    {
        return sp.GetRequiredService<TestDbContext>();
    }

    protected static async Task SaveChanges(IServiceProvider sp)
    {
        await GetDb(sp).SaveChangesAsync();
    }
}