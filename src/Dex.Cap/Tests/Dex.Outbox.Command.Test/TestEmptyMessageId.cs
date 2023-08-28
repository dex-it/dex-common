using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestEmptyMessageId : IOutboxMessage
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public Guid MessageId { get; }
    }
}