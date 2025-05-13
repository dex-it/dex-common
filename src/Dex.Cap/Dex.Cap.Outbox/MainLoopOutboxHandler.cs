using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox;

internal sealed class MainLoopOutboxHandler<TDbContext> : IOutboxHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxDataProvider<TDbContext> _dataProvider;
    private readonly IOutboxMetricCollector _metricCollector;
    private readonly ILogger<MainLoopOutboxHandler<TDbContext>> _logger;
    private readonly IOptions<OutboxOptions> _options;

    public MainLoopOutboxHandler(
        IServiceProvider serviceProvider,
        IOutboxDataProvider<TDbContext> dataProvider,
        IOutboxMetricCollector metricCollector,
        ILogger<MainLoopOutboxHandler<TDbContext>> logger,
        IOptions<OutboxOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _metricCollector = metricCollector ?? throw new ArgumentNullException(nameof(metricCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options;
    }

    private const string NoMessagesToProcess = "No messages to process";

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Outbox processor has been started");

        var jobs = await _dataProvider
            .GetWaitingJobs(cancellationToken)
            .ConfigureAwait(false);

        if (jobs.Length <= 0)
        {
            _metricCollector.IncEmptyProcessCount();
            _logger.LogDebug(NoMessagesToProcess);
            return;
        }

        // Semaphore to control the degree of parallelism
        using var semaphore = new SemaphoreSlim(_options.Value.ConcurrencyLimit);
        var tasks = jobs.Select(async job =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _metricCollector.IncProcessCount();

                using var activity = new Activity($"Process outbox message: {job.Envelope.Id}");
                activity.AddBaggage("Type", job.Envelope.MessageType);
                activity.AddBaggage("MessageId", job.Envelope.Id.ToString());

                if (!string.IsNullOrEmpty(job.Envelope.ActivityId))
                {
                    activity.SetParentId(job.Envelope.ActivityId!);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken, cancellationToken);

                activity.Start();
                _logger.LogDebug("Processing job - {Job}", job.Envelope.Id);
                _metricCollector.IncProcessJobCount();
                var sw = Stopwatch.StartNew();

                using var scope = _serviceProvider.CreateScope();
                var jobHandler = scope.ServiceProvider.GetRequiredService<IOutboxJobHandler>();
                await jobHandler.ProcessJob(job, cts.Token).ConfigureAwait(false);

                _metricCollector.AddProcessJobSuccessDuration(sw.Elapsed);
                _metricCollector.IncProcessJobSuccessCount();
                _logger.LogDebug("Job process completed - {Job}", job.Envelope.Id);
                activity.Stop();
            }
            finally
            {
                _logger.LogDebug("Outbox processor completed");
                job.Dispose();
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}