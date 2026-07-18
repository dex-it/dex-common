using System.Linq;
using System.Runtime.Loader;
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
    /// Внутри одного контекста загрузки каждый тип обязан прийти ровно один раз.
    /// </summary>
    /// <remarks>
    /// Инвариант заявлен про контекст загрузки, а не про процесс, и это не смягчение проверки, а её
    /// правильная формулировка. Дубли МЕЖДУ контекстами делает среда: сборка, загруженная в два
    /// AssemblyLoadContext (так работает тест-раннер Rider), даёт два неравных Type с общим
    /// AssemblyQualifiedName. Источник обязан отдавать их как есть, иначе он соврёт реестру, а тот на такой
    /// паре обязан упасть. Дефект источника это повтор ВНУТРИ одного контекста, его и сторожим.
    /// <para>
    /// Distinct() по ссылке здесь бесполезен в принципе: у копий из разных контекстов ссылки разные, и пара
    /// проходит его молча. Поэтому сравнение идёт по AssemblyQualifiedName.
    /// </para>
    /// <para>
    /// Разбиение по контекстам заменяет пропуск теста под ALC-изолирующим раннером: проверка работает всюду,
    /// ложных срабатываний от среды не даёт и знать про конкретный раннер не требует. Поведение РЕЕСТРА на
    /// таком дубле сторожит отдельный тест
    /// (<c>InboxStartupValidationTests.StartHost_SameMessageTypeLoadedTwice_ReportsTheDuplicateLoadInsteadOfAConflict</c>).
    /// </para>
    /// </remarks>
    [Test]
    public void GetMessageTypes_DoesNotReturnDuplicates()
    {
        var types = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance)
            .GetMessageTypes()
            .ToArray();

        foreach (var perContext in types.GroupBy(x => AssemblyLoadContext.GetLoadContext(x.Assembly)))
        {
            var identities = perContext.Select(x => x.AssemblyQualifiedName).ToArray();

            NUnit.Framework.Legacy.CollectionAssert.AllItemsAreUnique(identities);
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