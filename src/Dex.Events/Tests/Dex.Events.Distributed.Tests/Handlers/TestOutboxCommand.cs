using Dex.Cap.Common.Interfaces;

namespace Dex.Events.Distributed.Tests.Handlers;

public class TestOutboxCommand : IOutboxMessage
{
    public string Args { get; set; }

    public static string OutboxTypeId => nameof(TestOutboxCommand);
}