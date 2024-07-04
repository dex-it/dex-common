using System;
using Dex.Cap.Outbox.RetryStrategies;

namespace Dex.Cap.Outbox.Extensions
{
    public static class OutboxRetryStrategyConfiguratorExtension
    {
        public static void UseOutboxIncrementalRetryStrategy(this OutboxRetryStrategyConfigurator configurator, TimeSpan interval)
        {
            ArgumentNullException.ThrowIfNull(configurator);

            configurator.RetryStrategy = new IncrementalOutboxRetryStrategy(interval);
        }
    }
}