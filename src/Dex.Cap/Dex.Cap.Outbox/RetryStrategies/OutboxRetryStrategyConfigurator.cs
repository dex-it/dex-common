using System;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.RetryStrategies
{
    public sealed class OutboxRetryStrategyConfigurator
    {
        private IOutboxRetryStrategy _retryStrategy = new DefaultOutboxRetryStrategy();

        public IOutboxRetryStrategy RetryStrategy
        {
            get => _retryStrategy;
            set => _retryStrategy = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}