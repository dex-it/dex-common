using System;
using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Common.Ef.Interfaces;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxMessage : IHaveIdempotenceKey
    {
        Guid MessageId { get; }
        
        [SuppressMessage("Design", "CA1033:Методы интерфейса должны быть доступны для вызова дочерним типам")]
        string IHaveIdempotenceKey.IdempotentKey => MessageId.ToString("N");
    }
}