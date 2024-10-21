using System;
using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessage : IIdempotentKey
    {
        Guid MessageId { get; }
        
        [SuppressMessage("Design", "CA1033:Методы интерфейса должны быть доступны для вызова дочерним типам")]
        string IIdempotentKey.IdempotentKey => MessageId.ToString("N");
    }
}