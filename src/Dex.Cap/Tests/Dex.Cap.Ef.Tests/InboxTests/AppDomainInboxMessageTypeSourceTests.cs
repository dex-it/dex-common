using System;
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

    [Test]
    public void GetMessageTypes_DoesNotReturnDuplicates()
    {
        var types = new AppDomainInboxMessageTypeSource(NullLogger<AppDomainInboxMessageTypeSource>.Instance)
            .GetMessageTypes()
            .ToArray();

        Assert.AreEqual(types.Length, types.Distinct().Count());
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
