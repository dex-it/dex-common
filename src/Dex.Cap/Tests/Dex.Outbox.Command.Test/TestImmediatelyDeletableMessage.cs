using Dex.Cap.Common.Interfaces;
using JetBrains.Annotations;

namespace Dex.Outbox.Command.Test;

[UsedImplicitly]
public class TestImmediatelyDeletableMessage : IOutboxMessage
{
    public static string OutboxTypeId => nameof(TestImmediatelyDeletableMessage);

    public static bool DeleteImmediately => true;
}