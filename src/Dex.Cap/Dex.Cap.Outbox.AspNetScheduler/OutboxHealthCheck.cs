using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.AspNetScheduler;

internal class OutboxHealthCheck(IOptions<OutboxHandlerOptions> options, IOutboxStatistic outboxStatistic) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var lastCycle = outboxStatistic.GetLastStamp();
        var isHealthy = DateTime.UtcNow - lastCycle < options.Value.Period * 2;

        return Task.FromResult(isHealthy
            ? HealthCheckResult.Healthy("A healthy result.")
            : HealthCheckResult.Degraded($"The Outbox service is degraded. Last processed job was at {lastCycle}."));
    }
}