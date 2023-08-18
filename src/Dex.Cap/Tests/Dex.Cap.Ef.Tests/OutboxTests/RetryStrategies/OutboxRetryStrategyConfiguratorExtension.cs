using System;
using Dex.Cap.Outbox.RetryStrategies;

namespace Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;

internal static class OutboxRetryStrategyConfiguratorExtension
{
    internal static void UseOutboxExponentialRetryStrategy(this OutboxRetryStrategyConfigurator configurator, TimeSpan interval)
    {
        configurator.RetryStrategy = new ExponentialOutboxRetryStrategy(interval);
    }
}