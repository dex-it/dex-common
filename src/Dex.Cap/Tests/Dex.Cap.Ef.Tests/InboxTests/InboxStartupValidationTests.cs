using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Ошибки конфигурации реестра типов обязаны ронять старт хоста. Если они всплывают позже,
/// их перехватит цикл фонового обработчика и превратит в LogCritical: хост поднят, инбокс мёртв.
/// </summary>
/// <remarks>
/// Типы-нарушители объявлены здесь и НЕ реализуют <see cref="IInboxMessage"/>: иначе реальный
/// дискавери по загруженным сборкам подхватил бы их и уронил все остальные тесты инбокса.
/// В реестр они попадают через подменённый <see cref="IInboxMessageTypeSource"/>.
/// </remarks>
public class InboxStartupValidationTests : BaseTest
{
    private const string SharedDiscriminator = "6B1D4C0E-2A3F-4E5D-9C8B-7A6F5E4D3C2B";

    [Test]
    public void StartHost_DiscriminatorConflict_FailsAtStartup()
    {
        using var host = BuildHost(typeof(FirstClaimant), typeof(SecondClaimant));

        var ex = NUnit.Framework.Assert.ThrowsAsync<DiscriminatorConflictException>(
            (Func<Task>)(async () => await host.StartAsync()));

        Assert.IsTrue(ex!.Message.Contains(SharedDiscriminator, StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(FirstClaimant), StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(SecondClaimant), StringComparison.Ordinal));
    }

    [TestCase(typeof(QuotedDiscriminator), '\'', TestName = "StartHost_DiscriminatorWithQuote_FailsAtStartup")]
    [TestCase(typeof(BracedDiscriminator), '{', TestName = "StartHost_DiscriminatorWithBrace_FailsAtStartup")]
    [TestCase(typeof(SpacedDiscriminator), ' ', TestName = "StartHost_DiscriminatorWithSpace_FailsAtStartup")]
    public void StartHost_DiscriminatorWithUnsupportedCharacter_FailsAtStartup(Type messageType, char offender)
    {
        // Кавычка ломает SQL выборки на стороне Postgres, фигурная скобка ещё раньше, на string.Format
        // внутри EF. Оба падали бы на каждом цикле фоновой обработки, поэтому ловим на старте.
        using var host = BuildHost(messageType);

        var ex = NUnit.Framework.Assert.ThrowsAsync<DiscriminatorResolveException>(
            (Func<Task>)(async () => await host.StartAsync()));

        Assert.IsTrue(ex!.Message.Contains($"unsupported character '{offender}'", StringComparison.Ordinal), ex.Message);
    }

    [Test]
    public void RegistryExceptions_AreCatchableAsInboxException()
    {
        // XML IInboxService обещает InboxException. Если бы эти типы наследовали голый Exception,
        // задокументированный catch их не поймал бы.
        Assert.IsTrue(typeof(InboxException).IsAssignableFrom(typeof(DiscriminatorResolveException)));
        Assert.IsTrue(typeof(InboxException).IsAssignableFrom(typeof(DiscriminatorConflictException)));
        Assert.IsTrue(typeof(InboxException).IsAssignableFrom(typeof(InboxLeaseLostException)));
    }

    [Test]
    public void StartHost_EmptyDiscriminator_FailsAtStartup()
    {
        using var host = BuildHost(typeof(EmptyDiscriminator));

        var ex = NUnit.Framework.Assert.ThrowsAsync<DiscriminatorResolveException>(
            (Func<Task>)(async () => await host.StartAsync()));

        Assert.IsTrue(ex!.Message.Contains(nameof(IInboxMessage.InboxTypeId), StringComparison.Ordinal));
    }

    [Test]
    public async Task StartHost_ValidRegistry_StartsAndBuildsRegistryEagerly()
    {
        using var host = BuildHost(typeof(TestInboxCommand));

        await host.StartAsync();

        // Реестр построен на старте, а не при первом обращении фонового обработчика.
        var provider = host.Services.GetRequiredService<IInboxTypeDiscriminatorProvider>();
        Assert.IsTrue(provider.CurrentDomainInboxMessageTypes.ContainsKey(TestInboxCommand.InboxTypeId));
        Assert.IsTrue(provider.SupportedDiscriminators.Contains(TestInboxCommand.InboxTypeId));

        await host.StopAsync();
    }

    private IHost BuildHost(params Type[] messageTypes)
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                AddLogging(services);
                services
                    .AddScoped(_ => new TestDbContext(DbName))
                    .AddInbox<TestDbContext>()
                    .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>();

                services.RemoveAll<IInboxMessageTypeSource>();
                services.AddSingleton<IInboxMessageTypeSource>(new FixedTypeSource(messageTypes));
            })
            .Build();
    }

    private sealed class FixedTypeSource(Type[] types) : IInboxMessageTypeSource
    {
        public IEnumerable<Type> GetMessageTypes() => types;
    }

    private sealed class FirstClaimant
    {
        public static string InboxTypeId => SharedDiscriminator;
    }

    private sealed class SecondClaimant
    {
        public static string InboxTypeId => SharedDiscriminator;
    }

    private sealed class QuotedDiscriminator
    {
        public static string InboxTypeId => "O'Brien";
    }

    private sealed class BracedDiscriminator
    {
        public static string InboxTypeId => "cmd-{0}";
    }

    private sealed class SpacedDiscriminator
    {
        public static string InboxTypeId => "order created";
    }

    private sealed class EmptyDiscriminator
    {
        public static string InboxTypeId => "  ";
    }
}
