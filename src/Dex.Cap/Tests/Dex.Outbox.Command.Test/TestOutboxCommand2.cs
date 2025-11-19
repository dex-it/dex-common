using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestOutboxCommand2 : IOutboxMessage
{
    public static string OutboxTypeId => "36003399-08FB-48E0-B52A-803883805DAA";

    public string Args { get; init; }
}