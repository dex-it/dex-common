using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageDisallowAutoPublish : IOutboxMessage
{
    public static bool AllowAutoPublishing => false;

    public string OutboxTypeId => nameof(TestEmptyMessageDisallowAutoPublish);
}