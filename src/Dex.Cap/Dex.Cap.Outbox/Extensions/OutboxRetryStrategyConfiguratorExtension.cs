using System;
using Dex.Cap.Outbox.RetryStrategies;

namespace Dex.Cap.Outbox.Extensions
{
    public static class OutboxRetryStrategyConfiguratorExtension
    {
        public static void UseOutboxIncrementalRetryStrategy(this OutboxRetryStrategyConfigurator configurator, TimeSpan interval)
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));

            configurator.RetryStrategy = new IncrementalOutboxRetryStrategy(interval);
        }
    }
}