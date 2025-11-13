using Dex.Cap.Common.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestEmptyMessageDisallowAutoPublish : IOutboxMessage
{
    public bool AllowAutoPublishing => false;

    public string OutboxTypeId => nameof(TestEmptyMessageDisallowAutoPublish);
}