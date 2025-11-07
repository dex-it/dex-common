using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestOutboxCommand2 : IOutboxMessage
{
    public static string OutboxMessageType => "36003399-08FB-48E0-B52A-803883805DAA";

    public string Args { get; init; }
}