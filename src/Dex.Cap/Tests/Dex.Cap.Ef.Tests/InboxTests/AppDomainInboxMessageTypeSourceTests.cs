using System.Linq;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Дискавери типов сообщений по загруженным сборкам. Отдельный от реестра тип именно потому, что зависит
/// от того, какие сборки загружены в процесс, а реестр от этого зависеть не должен.
/// </summary>
public class AppDomainInboxMessageTypeSourceTests
{
    [Test]
    public void GetMessageTypes_FindsConcreteMessagesOfLoadedAssemblies()
    {
        var types = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance)
            .GetMessageTypes()
            .ToArray();

        Assert.Contains(typeof(TestInboxCommand), types);
        Assert.Contains(typeof(TestErrorInboxCommand), types);
        Assert.Contains(typeof(TestInboxUserCommand), types);
    }

    [Test]
    public void GetMessageTypes_ReturnsOnlyInstantiableImplementations()
    {
        var types = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance)
            .GetMessageTypes()
            .ToArray();

        // Абстрактные, интерфейсы и открытые generic-типы восстановить из строки нельзя,
        // поэтому в реестр они попадать не должны.
        Assert.IsFalse(types.Contains(typeof(IInboxMessage)));

        foreach (var type in types)
        {
            Assert.IsTrue(typeof(IInboxMessage).IsAssignableFrom(type), $"{type} must implement IInboxMessage");
            Assert.IsFalse(type.IsAbstract, $"{type} must not be abstract");
            Assert.IsFalse(type.IsInterface, $"{type} must not be an interface");
            Assert.IsFalse(type.ContainsGenericParameters, $"{type} must be a closed type");
        }
    }

    /// <summary>
    /// Каждый тип обязан прийти один раз, причём по идентичности, а не по ссылке.
    /// </summary>
    /// <remarks>
    /// Distinct() по ссылке этот случай не ловит в принципе: сборка, загруженная в два контекста, даёт два
    /// НЕравных Type с одинаковым AssemblyQualifiedName, и такая пара проходит его молча. Проверка по
    /// идентичности сторожит именно её: реестр на такой паре обязан упасть, поэтому дискавери, отдавший
    /// её, это уже сломанный процесс.
    /// <para>
    /// Тест утверждает инвариант о процессе с ОДНИМ контекстом загрузки. Тест-раннер Rider (NUnit engine)
    /// держит тест-сборку в двух AssemblyLoadContext, и тогда дубли по AQN появляются штатно — не потому,
    /// что источник сломан, а потому, что окружение уже не то, для которого инвариант заявлен. Такое
    /// окружение тест определяет сам и пропускает себя (Ignore, а не Fail): под dotnet test (один контекст)
    /// он сторожит настоящую регрессию, под ALC-изолирующим раннером не шумит и не требует ручной настройки.
    /// </para>
    /// </remarks>
    [Test]
    public void GetMessageTypes_DoesNotReturnDuplicates()
    {
        SkipWhenAssemblyLoadedMoreThanOnce();

        var identities = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance)
            .GetMessageTypes()
            .Select(x => x.AssemblyQualifiedName)
            .ToArray();

        NUnit.Framework.Legacy.CollectionAssert.AllItemsAreUnique(identities);
    }

    /// <summary>
    /// Пропустить тест, если тест-сборка загружена в процесс больше одного раза.
    /// </summary>
    /// <remarks>
    /// Считаем по имени сборки, а не по ссылке: у копий из разных AssemblyLoadContext ссылки разные, а имя
    /// одно. Больше одной — окружение с ALC-изоляцией (раннер Rider), инвариант уникальности в нём заведомо
    /// нарушен не источником, а средой, и проверять нечего.
    /// </remarks>
    private static void SkipWhenAssemblyLoadedMoreThanOnce()
    {
        var selfName = typeof(AppDomainInboxMessageTypeSourceTests).Assembly.GetName().Name;

        var loadCount = System.AppDomain.CurrentDomain.GetAssemblies()
            .Count(a => string.Equals(a.GetName().Name, selfName, System.StringComparison.Ordinal));

        if (loadCount > 1)
        {
            NUnit.Framework.Assert.Ignore(
                $"Test assembly '{selfName}' is loaded {loadCount} times (ALC-isolating runner). " +
                "The uniqueness invariant holds only for a single load context; skipping.");
        }
    }

    [Test]
    public void GetMessageTypes_IsStableAcrossCalls()
    {
        var source = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance);

        // Источник не кэширует: кэширует реестр. Но результат обязан быть воспроизводимым,
        // иначе реестр зависел бы от того, кто дёрнул его первым.
        NUnit.Framework.Legacy.CollectionAssert.AreEquivalent(source.GetMessageTypes().ToArray(), source.GetMessageTypes().ToArray());
    }
}