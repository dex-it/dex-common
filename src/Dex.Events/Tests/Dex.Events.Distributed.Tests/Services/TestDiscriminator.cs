using Dex.Events.Distributed.OutboxExtensions;
using Dex.Events.Distributed.Tests.Events;
using Dex.Events.Distributed.Tests.Handlers;
using MassTransit;

namespace Dex.Events.Distributed.Tests.Services;

public class TestDiscriminator : DistributedEventOutboxDiscriminator
{
    public TestDiscriminator()
    {
        Add<TestOutboxCommand>("ADE7BBE3-1C61-48C4-96B9-CFE0B8E1D4CA");
        Add<OnUserAdded>("DD04B28D-AE27-4CFF-9DA7-BEB1BA3F3EF2");
    }
}

public class TestDiscriminator<TBus> : DistributedEventOutboxDiscriminator<TBus>
    where TBus : IBus
{
    public TestDiscriminator()
    {
        Add<TestOutboxCommand>("ADE7BBE3-1C61-48C4-96B9-CFE0B8E1D4CA");
        Add<OnUserAdded>("DD04B28D-AE27-4CFF-9DA7-BEB1BA3F3EF2");
    }
}