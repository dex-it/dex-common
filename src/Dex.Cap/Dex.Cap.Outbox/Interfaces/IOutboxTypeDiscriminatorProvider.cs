using System;
using System.Collections.Frozen;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminatorProvider
{
    /// <summary>
    /// Дискриминаторы, обработку которых поддерживает текущий сервис
    /// </summary>
    FrozenSet<string> GetSupportedDiscriminators();

    /// <summary>
    /// Все дискриминаторы, известные этому сервису
    /// </summary>
    FrozenDictionary<string, Type> CurrentDomainOutboxMessageTypes { get; }
}