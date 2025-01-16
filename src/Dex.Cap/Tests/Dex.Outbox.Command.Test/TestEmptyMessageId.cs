using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    public Guid MessageId { get; init; }
}