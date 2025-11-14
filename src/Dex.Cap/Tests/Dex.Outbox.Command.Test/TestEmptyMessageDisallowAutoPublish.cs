using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageDisallowAutoPublish : IOutboxMessage
{
    public static bool AllowAutoPublishing => false;

    public static string OutboxTypeId => nameof(TestEmptyMessageDisallowAutoPublish);
}