using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageId : IOutboxMessage
{
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public Guid MessageId { get; }
}