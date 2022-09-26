using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    internal class OutboxHealthCheck<TDbContext> : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _processTimeout = TimeSpan.FromMinutes(5);

        public OutboxHealthCheck(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // must use own scoped DbContext because another healthchecks may use _dbContext instance 
            // in another thread if it configured at Scope lifetime service
            using var scope = _scopeFactory.CreateScope();
            var dataProvider = scope.ServiceProvider.GetRequiredService<IOutboxDataProvider<TDbContext>>();

            // get oldest record if exists
            var recordAsArray = (await dataProvider.GetFreeMessages(1, cancellationToken)
                .ConfigureAwait(false)).ToArray();

            if (!recordAsArray.Any())
                return await Task.FromResult(HealthCheckResult.Healthy("A healthy result.")).ConfigureAwait(false);

            var oldestRecord = recordAsArray.First();
            var isHealthy = (DateTime.Now - oldestRecord.CreatedUtc) < _processTimeout;

            if (isHealthy)
                return await Task.FromResult(HealthCheckResult.Healthy("A healthy result.")).ConfigureAwait(false);

            return await Task.FromResult(HealthCheckResult.Unhealthy(
                    $"The Outbox service is unhealthy. First unprocessed job was created at {oldestRecord.CreatedUtc}."))
                .ConfigureAwait(false);
        }
    }
}