using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxHandler<TDbContext> : IOutboxHandler
    {
        private const string LockTimeoutMessage = "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}";
        private const string UserCanceledDbMessage = "Operation canceled due to user request";
        private const string UserCanceledMessageWithId = "Operation canceled due to user request. MessageId: {MessageId}";
        private const string NoMessagesToProcess = "No messages to process";
        private readonly IOutboxDataProvider _dataProvider;
        private readonly IOutboxMessageHandlerFactory _handlerFactory;
        private readonly IOutboxSerializer _serializer;
        private readonly ILogger<OutboxHandler<TDbContext>> _logger;

        public OutboxHandler(IOutboxDataProvider dataProvider, IOutboxMessageHandlerFactory messageHandlerFactory,
            IOutboxSerializer serializer, ILogger<OutboxHandler<TDbContext>> logger)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _handlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Outbox processor has been started");

            var enumerable = _dataProvider.GetWaitingJobs(cancellationToken);
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    do
                    {
                        var job = enumerator.Current;
                        try
                        {
                            using (Activity activity = new($"Process outbox message:{job.Envelope.Id}"))
                            {
                                activity.AddBaggage("Type", job.Envelope.MessageType);
                                activity.AddBaggage("MessageId", job.Envelope.Id.ToString());

                                if (job.Envelope.ActivityId != null)
                                {
                                    activity.SetParentId(job.Envelope.ActivityId);
                                }

                                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken, cancellationToken))
                                {
                                    activity.Start();
                                    await ProcessJob(job, cts.Token).ConfigureAwait(false);
                                    activity.Stop();
                                }
                            }
                        }
                        finally
                        {
                            job.Dispose();
                        }
                    } while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                    _logger.LogDebug("Outbox processor finished");
                }
                else
                {
                    _logger.LogTrace(NoMessagesToProcess);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        [DoesNotReturn]
        private static void ThrowCantResolve(string messageType)
        {
            throw new OutboxException($"Can't resolve type of message '{messageType}'");
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
                _logger.LogError(ex, ex.Message, job.Envelope.Id);
                await _dataProvider.Fail(job, cancellationToken, ex.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process {MessageId}", job.Envelope.Id);
                await _dataProvider.Fail(job, cancellationToken, ex.Message, ex).ConfigureAwait(false);
            }
        }

        private async Task UnlockJobOnUserCancel(IOutboxLockedJob job)
        {
            // Несмотря на запрос отмены пользователем, нам лучше освободить блокировку задачи за разумно малое время.
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken))
            {
                linked.CancelAfter(1_000);
                try
                {
                    await _dataProvider.Fail(job, linked.Token, UserCanceledDbMessage).ConfigureAwait(false);
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
        }

        /// <exception cref="OutboxException"/>
        private async Task ProcessJobCore(IOutboxLockedJob job, CancellationToken cancellationToken)
        {
            var messageType = Type.GetType(job.Envelope.MessageType);
            if (messageType == null)
            {
                ThrowCantResolve(job.Envelope.MessageType);
            }

            var msg = _serializer.Deserialize(messageType, job.Envelope.Content);
            _logger.LogDebug("Message to processed: {Message}", msg);

            if (msg is IOutboxMessage outboxMessage)
            {
                await ProcessOutboxMessageCore(outboxMessage, cancellationToken).ConfigureAwait(false);
                await _dataProvider.Succeed(job, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                job.Envelope.Retries = int.MaxValue;
                ThrowUnableCast(job.Envelope.MessageType);
            }
        }

        private async Task ProcessOutboxMessageCore(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            var handler = _handlerFactory.GetMessageHandler(outboxMessage);
            try
            {
                await handler.ProcessMessage(outboxMessage, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (handler is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}