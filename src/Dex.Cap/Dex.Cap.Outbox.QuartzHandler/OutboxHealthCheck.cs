﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.AspNetScheduler
{
    internal class OutboxHealthCheck : IHealthCheck
    {
        private readonly IOptions<OutboxHandlerOptions> _options;
        private readonly IOutboxStatistic _outboxStatistic;

        public OutboxHealthCheck(IOptions<OutboxHandlerOptions> options, IOutboxStatistic outboxStatistic)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outboxStatistic = outboxStatistic ?? throw new ArgumentNullException(nameof(outboxStatistic));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var lastCycle = _outboxStatistic.GetLastStamp();
            var isHealthy = DateTime.UtcNow - lastCycle < _options.Value.Period * 2;

            if (isHealthy)
                return await Task.FromResult(HealthCheckResult.Healthy("A healthy result."));

            return await Task.FromResult(HealthCheckResult.Unhealthy($"The Outbox service is unhealthy. Last processed job was at {lastCycle}."));
        }
    }
}