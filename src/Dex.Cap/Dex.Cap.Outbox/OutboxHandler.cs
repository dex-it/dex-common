using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxHandler<TDbContext>(
        IServiceProvider serviceProvider,
        IOutboxDataProvider<TDbContext> dataProvider,
        IOutboxSerializer serializer,
        IOutboxMetricCollector metricCollector,
        IOutboxTypeDiscriminator discriminator,
        ILogger<OutboxHandler<TDbContext>> logger)
        : IOutboxHandler
    {
        private const string LockTimeoutMessage = "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}";
        private const string UserCanceledDbMessage = "Operation canceled due to user request";
        private const string UserCanceledMessageWithId = "Operation canceled due to user request. MessageId: {MessageId}";
        private const string NoMessagesToProcess = "No messages to process";

        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly IOutboxDataProvider<TDbContext> _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IOutboxSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        private readonly IOutboxMetricCollector _metricCollector = metricCollector ?? throw new ArgumentNullException(nameof(metricCollector));
        private readonly IOutboxTypeDiscriminator _discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
        private readonly ILogger<OutboxHandler<TDbContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Outbox processor has been started");

            var jobs = await _dataProvider.GetWaitingJobs(cancellationToken)
                .ConfigureAwait(false);

            if (jobs.Length > 0)
            {
                foreach (var job in jobs)
                {
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

                        await ProcessJob(job, cts.Token).ConfigureAwait(false);

                        _metricCollector.AddProcessJobSuccessDuration(sw.Elapsed);
                        _metricCollector.IncProcessJobSuccessCount();
                        _logger.LogDebug("Job process completed - {Job}", job.Envelope.Id);
                        activity.Stop();
                    }
                    finally
                    {
                        _logger.LogDebug("Outbox processor completed");
                        job.Dispose();
                    }
                }
            }
            else
            {
                _metricCollector.IncEmptyProcessCount();
                _logger.LogDebug(NoMessagesToProcess);
            }
        }

        [DoesNotReturn]
        private static void ThrowUnableCast(string messageType)
        {
            throw new OutboxException($"Message '{messageType}' are not of '{nameof(IOutboxMessage)}' type");
        }

        /// <exception cref="OperationCanceledException"/>
        private async Task ProcessJob(IOutboxLockedJob job, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message has been started to process {MessageId}", job.Envelope.Id);

            try
            {
                await ProcessJobCore(job, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Message {MessageId} has been processed", job.Envelope.Id);
            }
            catch (OperationCanceledException) when (job.LockToken.IsCancellationRequested)
                // Истекло время аренды блокировки.
            {
                _logger.LogError(LockTimeoutMessage, job.Envelope.Id);
                await _dataProvider.JobFail(job, default, "Lock is expired").ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!job.LockToken.IsCancellationRequested && cancellationToken.IsCancellationRequested)
                // Пользователь запросил отмену.
            {
                _logger.LogError(UserCanceledMessageWithId, job.Envelope.Id);
                await UnlockJobOnUserCancel(job).ConfigureAwait(false); // Попытаться освободить блокировку раньше чем истечёт время аренды.
                throw;
            }
            catch (OutboxException ex)
            {
                _logger.LogError(ex, "EnvelopeID: {MessageId} ", job.Envelope.Id);
                await _dataProvider.JobFail(job, default, ex.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process {MessageId}", job.Envelope.Id);
                await _dataProvider.JobFail(job, default, ex.Message, ex).ConfigureAwait(false);
            }
        }

        private async Task UnlockJobOnUserCancel(IOutboxLockedJob job)
        {
            // Несмотря на запрос отмены пользователем, нам лучше освободить блокировку задачи за разумно малое время.
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken);
            linked.CancelAfter(1_000);
            try
            {
                await _dataProvider.JobFail(job, linked.Token, UserCanceledDbMessage).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!job.LockToken.IsCancellationRequested)
            {
                _logger.LogError("Could not release the message for 1 second after the user cancellation request. MessageId: {MessageId}", job.Envelope.Id);
            }
            catch (OperationCanceledException)
            {
                // У блокировки истекло время жизни. Можно считать что освобождение выполнено успешно.
            }
        }

        /// <exception cref="OutboxException"/>
        /// <exception cref="DiscriminatorResolveTypeException"/>
        private async Task ProcessJobCore(IOutboxLockedJob job, CancellationToken cancellationToken)
        {
            var messageType = _discriminator.ResolveType(job.Envelope.MessageType);
            var msg = _serializer.Deserialize(messageType, job.Envelope.Content);
            _logger.LogDebug("Message to processed: {Message}", msg);

            if (msg is IOutboxMessage outboxMessage)
            {
                if (msg is not EmptyOutboxMessage)
                {
                    await ProcessOutboxMessageScoped(outboxMessage, cancellationToken).ConfigureAwait(false);
                }

                await _dataProvider.JobSucceed(job, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                job.Envelope.Retries = int.MaxValue;
                ThrowUnableCast(job.Envelope.MessageType);
            }
        }

        private async Task ProcessOutboxMessageScoped(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var handlerFactory = scope.ServiceProvider.GetRequiredService<IOutboxMessageHandlerFactory>();
            var handler = handlerFactory.GetMessageHandler(outboxMessage);

            try
            {
                await handler.ProcessMessage(outboxMessage, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (handler is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}