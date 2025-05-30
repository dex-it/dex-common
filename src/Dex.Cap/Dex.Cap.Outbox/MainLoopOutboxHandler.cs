﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox
{
    internal sealed class MainLoopOutboxHandler<TDbContext>(
        IServiceProvider serviceProvider,
        IOutboxDataProvider dataProvider,
        IOutboxMetricCollector metricCollector,
        IOptions<OutboxOptions> options,
        ILogger<MainLoopOutboxHandler<TDbContext>> logger) : IOutboxHandler
    {
        private const string NoMessagesToProcess = "No messages to process";

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Outbox processor has been started");

            var jobs = await dataProvider
                .GetWaitingJobs(cancellationToken)
                .ConfigureAwait(false);

            if (jobs.Length <= 0)
            {
                metricCollector.IncEmptyProcessCount();
                logger.LogDebug(NoMessagesToProcess);
                return;
            }

            // Semaphore to control the degree of parallelism
            using var semaphore = new SemaphoreSlim(options.Value.ConcurrencyLimit);
            var tasks = jobs.Select(async job =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    metricCollector.IncProcessCount();

                    using var activity = new Activity($"Process outbox message: {job.Envelope.Id}");
                    activity.AddBaggage("Type", job.Envelope.MessageType);
                    activity.AddBaggage("MessageId", job.Envelope.Id.ToString());

                    if (!string.IsNullOrEmpty(job.Envelope.ActivityId))
                    {
                        activity.SetParentId(job.Envelope.ActivityId!);
                    }

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken, cancellationToken);

                    activity.Start();
                    logger.LogDebug("Processing job - {Job}", job.Envelope.Id);
                    metricCollector.IncProcessJobCount();
                    var sw = Stopwatch.StartNew();

                    using var scope = serviceProvider.CreateScope();
                    var jobHandler = scope.ServiceProvider.GetRequiredService<IOutboxJobHandler>();
                    await jobHandler.ProcessJob(job, cts.Token).ConfigureAwait(false);

                    metricCollector.AddProcessJobSuccessDuration(sw.Elapsed);
                    metricCollector.IncProcessJobSuccessCount();
                    logger.LogDebug("Job process completed - {Job}", job.Envelope.Id);
                    activity.Stop();
                }
                finally
                {
                    logger.LogDebug("Outbox processor completed");
                    job.Dispose();
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}