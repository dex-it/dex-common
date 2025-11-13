using Dex.Cap.Common.Interfaces;

namespace Dex.Events.Distributed.Tests.Handlers;

public class TestOutboxCommand : IOutboxMessage
{
    public string Args { get; set; }

    public string OutboxTypeId => nameof(TestOutboxCommand);
}