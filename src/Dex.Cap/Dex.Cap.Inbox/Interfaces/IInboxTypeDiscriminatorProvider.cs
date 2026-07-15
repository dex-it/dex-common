using System;
using System.Collections.Frozen;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxTypeDiscriminatorProvider
{
    /// <summary>
    /// Все типы сообщений инбокса, найденные в загруженных сборках, по их дискриминаторам.
    /// </summary>
    FrozenDictionary<string, Type> CurrentDomainInboxMessageTypes { get; }

    /// <summary>
    /// Дискриминаторы, для которых в текущем сервисе зарегистрирован обработчик.
    /// </summary>
    /// <remarks>Только они выбираются на обработку: одна таблица может обслуживать несколько потребителей.</remarks>
    FrozenSet<string> SupportedDiscriminators { get; }

    /// <summary>
    /// Построить реестр немедленно, чтобы ошибки конфигурации проявились на старте хоста, а не в рантайме.
    /// </summary>
    void Warmup();
}
