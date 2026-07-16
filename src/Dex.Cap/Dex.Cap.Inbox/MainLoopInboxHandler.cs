using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Jobs;
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

    /// <inheritdoc />
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

        using var semaphore = new SemaphoreSlim(options.Value.ConcurrencyLimit);
        var tasks = jobs.Select(job => ProcessJob(job, semaphore, cancellationToken)).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        logger.LogDebug("Inbox processor completed");

        return jobs.Length;
    }

    /// <summary>
    /// Обработать одно сообщение партии, соблюдая предел параллелизма.
    /// </summary>
    /// <remarks>
    /// Задача владеет собой на всё время метода, включая ожидание слота: остановка хоста гасит WaitAsync
    /// исключением, и без внешнего using взведённые таймеры аренды всех задач, не дождавшихся очереди,
    /// утекли бы. Слот освобождается только тем, кто его занял.
    /// </remarks>
    private async Task ProcessJob(IInboxLockedJob job, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        using (job)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (job.LockToken.IsCancellationRequested)
                {
                    SkipExpiredBeforeStart(job);
                    return;
                }

                metricCollector.IncProcessCount();

                using var activity = StartActivity(job);
                using var cts = CreateProcessingToken(job, cancellationToken);

                await InvokeJobHandler(job, cts.Token).ConfigureAwait(false);

                activity.Stop();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Отпустить сообщение, аренда которого истекла до начала обработки.
    /// </summary>
    /// <remarks>
    /// Аренда всей партии тикает с момента захвата, а задачи идут по ConcurrencyLimit за раз, поэтому
    /// хвост партии может дождаться очереди уже с мёртвой арендой. Обрабатывать такое сообщение нельзя:
    /// строка больше не наша. Штрафовать его тоже нельзя: обработчик его не видел, а попытки существуют,
    /// чтобы считать РЕАЛЬНЫЕ отказы обработки. Писать в строку тоже нечего: аренда истекает в БД сама, после
    /// чего сообщение возвращается в выборку и его заберёт следующий цикл.
    /// </remarks>
    private void SkipExpiredBeforeStart(IInboxLockedJob job)
    {
        metricCollector.IncExpiredBeforeStartCount();
        logger.LogWarning(
            "Inbox job {Job} is skipped: its lease expired while the batch was draining. " +
            "Increase LockTimeout or lower MessagesToProcess",
            job.Envelope.Id);
    }

    /// <summary>
    /// Открыть трассу обработки, продолжив трассу источника.
    /// </summary>
    /// <remarks>
    /// Идентификаторы источника идут тегами, а не baggage: baggage по определению уезжает за пределы
    /// процесса в заголовках исходящих вызовов, а MessageId здесь это внешнее значение (идентификатор
    /// сообщения брокера или Idempotency-Key HTTP-запроса), и светить им третьим сервисам, в которые
    /// сходит обработчик, незачем.
    /// <para>
    /// Без продолжения трассы источника фоновая обработка выглядела бы как не связанная с приёмом
    /// операция.
    /// </para>
    /// </remarks>
    private static Activity StartActivity(IInboxLockedJob job)
    {
        var activity = new Activity($"Process inbox message: {job.Envelope.Id}");
        activity.AddBaggage("Type", job.Envelope.MessageType);
        activity.AddBaggage("EnvelopeId", job.Envelope.Id.ToString());
        activity.AddTag("SourceMessageId", job.Envelope.MessageId);
        activity.AddTag("ConsumerId", job.Envelope.ConsumerId);

        if (string.IsNullOrEmpty(job.Envelope.ActivityId) is false)
        {
            activity.SetParentId(job.Envelope.ActivityId);
        }

        activity.Start();

        return activity;
    }

    /// <summary>
    /// Токен обработки: гаснет и по истечении аренды, и по остановке хоста.
    /// </summary>
    /// <remarks>
    /// Оба события гасят обработку одинаково: продолжать без аренды нельзя, её мог уже перехватить другой
    /// обработчик.
    /// </remarks>
    private static CancellationTokenSource CreateProcessingToken(IInboxLockedJob job, CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(job.LockToken, cancellationToken);

    /// <summary>
    /// Передать сообщение обработчику в собственном scope.
    /// </summary>
    private async Task InvokeJobHandler(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing job - {Job}", job.Envelope.Id);
        metricCollector.IncProcessJobCount();

        var duration = Stopwatch.StartNew();

        using var scope = serviceProvider.CreateScope();
        var jobHandler = scope.ServiceProvider.GetRequiredService<IInboxJobHandler>();
        await jobHandler.ProcessJob(job, cancellationToken).ConfigureAwait(false);

        metricCollector.AddProcessJobDuration(duration.Elapsed);
        logger.LogDebug("Job process completed - {Job}", job.Envelope.Id);
    }
}
