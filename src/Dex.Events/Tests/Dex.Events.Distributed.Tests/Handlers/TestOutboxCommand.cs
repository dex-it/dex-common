using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestOutboxCommand : IOutboxMessage
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public string Args { get; set; }
    }
}