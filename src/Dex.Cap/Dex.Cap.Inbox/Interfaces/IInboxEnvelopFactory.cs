using System;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxEnvelopFactory
{
    InboxEnvelope CreateEnvelop<T>(T message, InboxMessageIdentity identity, TimeSpan? lockTimeout = null)
        where T : class, IInboxMessage;
}