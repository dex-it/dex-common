using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Ef;

/// <summary>
/// Обработчик одного OutboxJob. Изолирует необходимые ресурсы для этого
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
internal class OutboxJobHandlerEf<TDbContext> : IOutboxJobHandler
    where TDbContext : DbContext
{
    private const string LockTimeoutMessage = "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}";
    private const string UserCanceledDbMessage = "Operation canceled due to user request";
    private const string UserCanceledMessageWithId = "Operation canceled due to user request. MessageId: {MessageId}";

    private readonly IOutboxDataProvider _dataProvider;
    private readonly IOutboxSerializer _serializer;
    private readonly IOutboxTypeDiscriminator _discriminator;
    private readonly IOutboxMessageHandlerFactory _handlerFactory;
    private readonly TDbContext _dbContext;
    private readonly ILogger<OutboxJobHandlerEf<TDbContext>> _logger;

    public OutboxJobHandlerEf(IOutboxDataProvider dataProvider,
        IOutboxSerializer serializer,
        IOutboxTypeDiscriminator discriminator,
        IOutboxMessageHandlerFactory handlerFactory,
        TDbContext dbContext,
        ILogger<OutboxJobHandlerEf<TDbContext>> logger)
    {
        _dataProvider = dataProvider;
        _serializer = serializer;
        _discriminator = discriminator;
        _handlerFactory = handlerFactory;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <exception cref="OperationCanceledException"/>
    public async Task ProcessJob(IOutboxLockedJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Message has been started to process {MessageId}", job.Envelope.Id);

        try
        {
            try
            {
                await ProcessJobCore(job, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Message {MessageId} has been processed", job.Envelope.Id);
            }
            finally
            {
                // Очищаем контекст от всего что там осталось после операции, т.к. переиспользуем его
                _dbContext.ChangeTracker.Clear();
            }
        }
        catch (OperationCanceledException ex) when (job.LockToken.IsCancellationRequested)
            // Истекло время аренды блокировки.
        {
            _logger.LogError(ex, LockTimeoutMessage, job.Envelope.Id);
            await _dataProvider.JobFail(job, default, "Lock is expired").ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!job.LockToken.IsCancellationRequested && cancellationToken.IsCancellationRequested)
            // Пользователь запросил отмену.
        {
            _logger.LogError(ex, UserCanceledMessageWithId, job.Envelope.Id);
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
        catch (OperationCanceledException ex) when (!job.LockToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Could not release the message for 1 second after the user cancellation request. MessageId: {MessageId}", job.Envelope.Id);
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
        cancellationToken.ThrowIfCancellationRequested();

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
        cancellationToken.ThrowIfCancellationRequested();

        var handler = _handlerFactory.GetMessageHandler(outboxMessage);
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

    [DoesNotReturn]
    private static void ThrowUnableCast(string messageType)
    {
        throw new OutboxException($"Message '{messageType}' are not of '{nameof(IOutboxMessage)}' type");
    }
}