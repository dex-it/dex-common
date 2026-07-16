using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.AspNetScheduler;

internal class InboxHealthCheck(IOptions<InboxHandlerOptions> options, IInboxStatistic inboxStatistic) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var lastCycle = inboxStatistic.GetLastStamp();
        var isHealthy = DateTime.UtcNow - lastCycle < options.Value.Period * 2;

        return Task.FromResult(isHealthy
            ? HealthCheckResult.Healthy("A healthy result.")
            : HealthCheckResult.Degraded($"The Inbox service is degraded. Last processed job was at {lastCycle}."));
    }
}