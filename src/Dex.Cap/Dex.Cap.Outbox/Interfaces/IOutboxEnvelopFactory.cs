using System;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxEnvelopFactory
{
    public OutboxEnvelope CreateEnvelop<T>(T message, Guid correlationId, DateTime? startAtUtc = null, TimeSpan? lockTimeout = null) where T : class, IOutboxMessage;
}