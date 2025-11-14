using Dex.Cap.Common.Interfaces;
using Dex.Events.Distributed.Tests.Models;

namespace Dex.Events.Distributed.Tests.Events;

public sealed class OnUserAdded : DistributedCustomerEventParams, IOutboxMessage
{
    public static string OutboxTypeId => nameof(OnUserAdded);
}