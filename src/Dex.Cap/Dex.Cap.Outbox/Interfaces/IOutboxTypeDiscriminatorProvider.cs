using System;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminatorProvider
{
    /// <summary>
    /// Дискриминаторы, обработку которых поддерживает текущий сервис
    /// </summary>
    FrozenSet<string> SupportedDiscriminators { get; }

    /// <summary>
    /// Дискриминаторы, у которых сообщения должны удаляться сразу после выполнения работы
    /// Рекомендуется для очень крупных и очень часто генерируемых сообщений
    /// </summary>
    ImmutableArray<string> ImmediatelyDeletableMessages { get; }

    /// <summary>
    /// Все дискриминаторы, известные этому сервису
    /// </summary>
    FrozenDictionary<string, Type> CurrentDomainOutboxMessageTypes { get; }
}