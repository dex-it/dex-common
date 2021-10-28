using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    internal class OutboxHealthCheck : IHealthCheck
    {
        private readonly IOutboxDataProvider _dataProvider;
        private readonly TimeSpan _processTimeout = TimeSpan.FromMinutes(5);

        public OutboxHealthCheck(IOutboxDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // get oldest record if exists
            var recordAsArray = (await _dataProvider.GetFreeMessages(1, cancellationToken)
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
