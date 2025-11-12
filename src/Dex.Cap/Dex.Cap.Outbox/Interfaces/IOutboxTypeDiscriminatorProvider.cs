using System;
using System.Collections.Frozen;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminatorProvider
{
    /// <summary>
    /// Дискриминаторы, обработку которых поддерживает текущий сервис
    /// </summary>
    /// <returns></returns>
    Task<FrozenSet<string>> GetSupportedDiscriminators();

    /// <summary>
    /// Все дискриминаторы, известные этому сервису
    /// </summary>
    FrozenDictionary<string, Type> CurrentDomainOutboxMessageTypes { get; }
}