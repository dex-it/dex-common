using System;
using System.Collections.Frozen;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminatorProvider
{
    /// <summary>
    /// Дискриминаторы, обработку которых поддерживает текущий сервис
    /// </summary>
    /// <returns></returns>
    FrozenSet<string> SupportedDiscriminators { get; }

    /// <summary>
    /// Все дискриминаторы, известные этому сервису
    /// </summary>
    FrozenDictionary<string, Type> CurrentDomainOutboxMessageTypes { get; }
}