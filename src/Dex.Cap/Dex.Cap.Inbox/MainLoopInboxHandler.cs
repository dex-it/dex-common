using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox;

internal sealed class MainLoopInboxHandler(
    IServiceProvider serviceProvider,
    IInboxDataProvider dataProvider,
    IInboxMetricCollector metricCollector,
    IOptions<InboxOptions> options,
    ILogger<MainLoopInboxHandler> logger) : IInboxHandler
{
    private const string NoMessagesToProcess = "No messages to process";

    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Inbox processor has been started");

        var jobs = await dataProvider
            .GetWaitingJobs(cancellationToken)
            .ConfigureAwait(false);

        if (jobs.Length <= 0)
        {
            metricCollector.IncEmptyProcessCount();
            logger.LogDebug(NoMessagesToProcess);
            return 0;
        }

        // Semaphore to control the degree of parallelism
        using var semaphore = new SemaphoreSlim(options.Value.ConcurrencyLimit);
        var tasks = jobs.Select(async job =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Аренда всей партии тикает с момента захвата, а джобы идут по ConcurrencyLimit за раз,
                // поэтому хвост партии может дождаться очереди уже с мёртвой арендой. Обрабатывать такое
                // сообщение нельзя: строка больше не наша. Но и штрафовать его нельзя - обработчик его
                // не видел, а попытки существуют, чтобы считать РЕАЛЬНЫЕ отказы обработки. Строку заберёт
                // следующий цикл, здесь достаточно её отпустить.
                if (job.LockToken.IsCancellationRequested)
                {
                    metricCollector.IncExpiredBeforeStartCount();
                    logger.LogWarning(
                        "Inbox job {Job} is skipped: its lease expired while the batch was draining. " +
                        "Increase LockTimeout or lower MessagesToProcess",
                        job.Envelope.Id);
                    return;
                }

                metricCollector.IncProcessCount();

                using var activity = new Activity($"Process inbox message: {job.Envelope.Id}");
                activity.AddBaggage("Type", job.Envelope.MessageType);
                activity.AddBaggage("MessageId", job.Envelope.MessageId);
                activity.AddBaggage("ConsumerId", job.Envelope.ConsumerId);

                // Продолжаем трассу источника: иначе фоновая обработка выглядит как не связанная с приёмом операция.
                if (string.IsNullOrEmpty(job.Envelope.ActivityId) is false)
                    activity.SetParentId(job.Envelope.ActivityId);

                // Аренда и остановка хоста гасят обработку одинаково: продолжать без аренды нельзя,
                // её мог уже перехватить другой обработчик.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken, cancellationToken);

                activity.Start();
                logger.LogDebug("Processing job - {Job}", job.Envelope.Id);
                metricCollector.IncProcessJobCount();
                var sw = Stopwatch.StartNew();

                using var scope = serviceProvider.CreateScope();
                var jobHandler = scope.ServiceProvider.GetRequiredService<IInboxJobHandler>();
                await jobHandler.ProcessJob(job, cts.Token).ConfigureAwait(false);

                metricCollector.AddProcessJobDuration(sw.Elapsed);
                logger.LogDebug("Job process completed - {Job}", job.Envelope.Id);
                activity.Stop();
            }
            finally
            {
                job.Dispose();
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        logger.LogDebug("Inbox processor completed");

        return jobs.Length;
    }
}
