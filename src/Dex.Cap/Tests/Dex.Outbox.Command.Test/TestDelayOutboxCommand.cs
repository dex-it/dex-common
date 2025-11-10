using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestDelayOutboxCommand : IOutboxMessage
{
    public static string OutboxTypeId => "ECF5C0E2-4490-4D7E-A177-3D888CD6EA0D";

    public int DelayMsec { get; init; }

    public string Args { get; init; }
}