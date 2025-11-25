using Dex.Cap.Common.Interfaces;
using JetBrains.Annotations;

namespace Dex.Outbox.Command.Test;

[UsedImplicitly]
public class TestOutboxMessage : IOutboxMessage
{
    public static string OutboxTypeId => nameof(TestOutboxMessage);
}