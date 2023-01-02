using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    public Guid MessageId { get; }
}