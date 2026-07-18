using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
    private const string DuplicateDiscriminator = "1F0E9D8C-7B6A-4C5D-8E9F-0A1B2C3D4E5F";

    [Test]
    public async Task StartHost_DiscriminatorConflict_FailsAtStartup()
    {
        using var host = BuildHost(typeof(FirstClaimant), typeof(SecondClaimant));

        var ex = await CaptureStartupFailure(host);

        Assert.IsInstanceOf<DiscriminatorConflictException>(ex);
        Assert.IsTrue(ex!.Message.Contains(SharedDiscriminator, StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(FirstClaimant), StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(SecondClaimant), StringComparison.Ordinal));
    }

    [Test]
    public async Task StartHost_InheritedDiscriminator_ReportsTheConflictHonestly()
    {
        // Производный тип наследует статический дискриминатор базового. Компилятор такой код принимает,
        // дискавери подхватывает оба типа, и честный исход это конфликт: два типа заявили один
        // идентификатор.
        using var host = BuildHost(typeof(BaseClaimant), typeof(InheritingClaimant));

        var ex = await CaptureStartupFailure(host);

        Assert.IsInstanceOf<DiscriminatorConflictException>(ex);
        Assert.IsTrue(ex!.Message.Contains(nameof(BaseClaimant), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(nameof(InheritingClaimant), StringComparison.Ordinal), ex.Message);
    }

    [Test]
    public void RegistryExceptions_AreCaughtAsInboxException()
    {
        // XML IInboxService обещает InboxException. Проверяем ПОВЕДЕНИЕ, а не иерархию типов: реально ли
        // задокументированный catch (InboxException) ловит каждое из этих исключений. Если бы какое-то
        // наследовало голый Exception, catch его пропустил бы, и тест обязан упасть на этом.
        AssertCaughtAsInboxException(new DiscriminatorResolveException("registry error"));
        AssertCaughtAsInboxException(new DiscriminatorConflictException("registry error"));
        AssertCaughtAsInboxException(new InboxLeaseLostException("registry error"));
        AssertCaughtAsInboxException(new AmbiguousMessageTypeException("registry error"));

        static void AssertCaughtAsInboxException(Exception thrown)
        {
            try
            {
                throw thrown;
            }
            catch (InboxException caught)
            {
                Assert.AreSame(thrown, caught, "the exception must be caught as InboxException, not rethrown");
            }
        }
    }

    /// <summary>
    /// Сборка, загруженная в процесс дважды, это не конфликт дискриминаторов: тип заявлен один, а CLR-типов
    /// стало два. Исход обязан называть настоящую причину, иначе сообщение обвиняет типы сообщений в том,
    /// чего они не делали, и чинить его идут не туда.
    /// </summary>
    /// <remarks>
    /// Дубль воспроизводится эмитом двух сборок с одной идентичностью, а НЕ вторым AssemblyLoadContext:
    /// реально загруженная второй раз сборка осталась бы в домене и уронила бы весь остальной набор тестов
    /// по порядку выполнения, то есть тест воспроизвёл бы ровно тот баг, который сторожит. Эмит даёт ту же
    /// пару (AssemblyQualifiedName совпадают, Type не равны) и при этом невидим для дискавери: тот
    /// отбрасывает динамические сборки.
    /// </remarks>
    [Test]
    public async Task StartHost_SameMessageTypeLoadedTwice_ReportsTheDuplicateLoadInsteadOfAConflict()
    {
        var first = EmitDuplicateMessageType();
        var second = EmitDuplicateMessageType();

        Assert.AreEqual(first.AssemblyQualifiedName, second.AssemblyQualifiedName, "стенд обязан повторить идентичность дубля");
        Assert.AreNotEqual(first, second, "стенд обязан дать два разных CLR-типа");

        using var host = BuildHost(first, second);

        var ex = await CaptureStartupFailure(host);

        Assert.IsInstanceOf<AmbiguousMessageTypeException>(ex);
        Assert.IsTrue(ex!.Message.Contains("loaded into this process more than once", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(DuplicateDiscriminator, StringComparison.Ordinal), ex.Message);

        // Разбирать такое будут по контексту загрузки и файлу: без них сообщение не указывает, кого искать.
        Assert.IsTrue(ex.Message.Contains("load context", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Тот же самый тип, отданный источником дважды, отображение не меняет, поэтому и отказом не является:
    /// выбирать не из чего.
    /// </summary>
    [Test]
    public async Task StartHost_SourceReturnsTheSameTypeTwice_StartsWithoutError()
    {
        using var host = BuildHost(typeof(TestInboxCommand), typeof(TestInboxCommand));

        var ex = await CaptureStartupFailure(host);

        Assert.IsNull(ex, ex?.Message ?? string.Empty);

        await host.StopAsync();
    }

    [Test]
    public async Task StartHost_EmptyDiscriminator_FailsAtStartup()
    {
        using var host = BuildHost(typeof(EmptyDiscriminator));

        var ex = await CaptureStartupFailure(host);

        Assert.IsInstanceOf<DiscriminatorResolveException>(ex);
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

    /// <summary>
    /// Запустить хост и вернуть исключение старта (или null, если старт прошёл). Host передаётся
    /// параметром, а не захватывается замыканием: иначе using-переменная утекла бы в делегат
    /// (ReSharper AccessToDisposedClosure), хотя здесь это и безопасно.
    /// </summary>
    private static async Task<Exception?> CaptureStartupFailure(IHost host)
    {
        try
        {
            await host.StartAsync();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Собрать тип сообщения в динамической сборке с фиксированной идентичностью.
    /// </summary>
    /// <remarks>
    /// Имя и версия сборки заданы константами, поэтому два вызова дают ровно то, что даёт одна сборка,
    /// загруженная в два контекста: одинаковый AssemblyQualifiedName при неравных Type. Дискриминатор
    /// объявлен статическим свойством, как того требует контракт, и читается тем же путём, что у обычного
    /// типа.
    /// </remarks>
    private static Type EmitDuplicateMessageType()
    {
        var assemblyName = new AssemblyName("Dex.Cap.Ef.Tests.EmittedDuplicate") { Version = new Version(1, 0, 0, 0) };

        var module = AssemblyBuilder
            .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
            .DefineDynamicModule(assemblyName.Name!);

        var type = module.DefineType("Dex.Cap.Ef.Tests.Emitted.DuplicateCommand", TypeAttributes.Public | TypeAttributes.Class);

        var getter = type.DefineMethod(
            "get_" + nameof(IInboxMessage.InboxTypeId),
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
            typeof(string),
            Type.EmptyTypes);

        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldstr, DuplicateDiscriminator);
        il.Emit(OpCodes.Ret);

        type.DefineProperty(nameof(IInboxMessage.InboxTypeId), PropertyAttributes.None, typeof(string), null)
            .SetGetMethod(getter);

        return type.CreateType();
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
        // ReSharper disable once UnusedMember.Local
        public static string InboxTypeId => SharedDiscriminator;
    }

    private sealed class SecondClaimant
    {
        // ReSharper disable once UnusedMember.Local
        public static string InboxTypeId => SharedDiscriminator;
    }

    private class BaseClaimant
    {
        // ReSharper disable once UnusedMember.Local
        public static string InboxTypeId => SharedDiscriminator;
    }

    private sealed class InheritingClaimant : BaseClaimant;

    private sealed class EmptyDiscriminator
    {
        // ReSharper disable once UnusedMember.Local
        public static string InboxTypeId => "  ";
    }
}