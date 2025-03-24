using System;
using Dex.Cap.Outbox.RetryStrategies;

namespace Dex.Cap.Outbox.Extensions;

public static class OutboxReSchedulingIncrementalStrategyConfiguratorExtension
{
    public static void UseOutboxReSchedulingIncrementalRetryStrategy(this OutboxRetryStrategyConfigurator configurator,
        TimeSpan interval, TimeSpan reschedulingInterval)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.RetryStrategy = new ReschedulingIncrementalOutboxRetryStrategy(interval, reschedulingInterval);
    }
}