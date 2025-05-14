using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
/// <param name="dataProvider"></param>
/// <param name="serializer"></param>
/// <param name="discriminator"></param>
/// <param name="handlerFactory"></param>
/// <param name="dbContext"></param>
/// <param name="logger"></param>
internal class OutboxJobHandlerEf<TDbContext>(
    IOutboxDataProvider dataProvider,
    IOutboxSerializer serializer,
    IOutboxTypeDiscriminator discriminator,
    IOutboxMessageHandlerFactory handlerFactory,
    TDbContext dbContext,
    ILogger<OutboxJobHandlerEf<TDbContext>> logger) : IOutboxJobHandler
    where TDbContext : DbContext
{
    private const string LockTimeoutMessage = "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}";
    private const string UserCanceledDbMessage = "Operation canceled due to user request";
    private const string UserCanceledMessageWithId = "Operation canceled due to user request. MessageId: {MessageId}";

    /// <exception cref="OperationCanceledException"/>
    public async Task ProcessJob(IOutboxLockedJob job, CancellationToken cancellationToken)
    {
        logger.LogDebug("Message has been started to process {MessageId}", job.Envelope.Id);

        try
        {
            try
            {
                await ProcessJobCore(job, cancellationToken).ConfigureAwait(false);
                logger.LogDebug("Message {MessageId} has been processed", job.Envelope.Id);
            }
            finally
            {
                // Очищаем контекст от всего что там осталось после операции, т.к. переиспользуем его
                dbContext.ChangeTracker.Clear();
            }
        }
        catch (OperationCanceledException oce) when (job.LockToken.IsCancellationRequested)
            // Истекло время аренды блокировки.
        {
            logger.LogError(oce, LockTimeoutMessage, job.Envelope.Id);
            await dataProvider.JobFail(job, default, "Lock is expired").ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (!job.LockToken.IsCancellationRequested && cancellationToken.IsCancellationRequested)
            // Пользователь запросил отмену.
        {
            logger.LogError(oce, UserCanceledMessageWithId, job.Envelope.Id);
            await UnlockJobOnUserCancel(job).ConfigureAwait(false); // Попытаться освободить блокировку раньше чем истечёт время аренды.
            throw;
        }
        catch (OutboxException ex)
        {
            logger.LogError(ex, "EnvelopeID: {MessageId} ", job.Envelope.Id);
            await dataProvider.JobFail(job, default, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {MessageId}", job.Envelope.Id);
            await dataProvider.JobFail(job, default, ex.Message, ex).ConfigureAwait(false);
        }
    }

    private async Task UnlockJobOnUserCancel(IOutboxLockedJob job)
    {
        // Несмотря на запрос отмены пользователем, нам лучше освободить блокировку задачи за разумно малое время.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(job.LockToken);
        linked.CancelAfter(1_000);
        try
        {
            await dataProvider.JobFail(job, linked.Token, UserCanceledDbMessage).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (!job.LockToken.IsCancellationRequested)
        {
            logger.LogError(oce, "Could not release the message for 1 second after the user cancellation request. MessageId: {MessageId}", job.Envelope.Id);
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

        var messageType = discriminator.ResolveType(job.Envelope.MessageType);
        var msg = serializer.Deserialize(messageType, job.Envelope.Content);
        logger.LogDebug("Message to processed: {Message}", msg);

        if (msg is not null)
        {
            if (msg is not EmptyOutboxMessage)
            {
                await ProcessOutboxMessageScoped(msg, cancellationToken).ConfigureAwait(false);
            }

            await dataProvider.JobSucceed(job, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            job.Envelope.Retries = int.MaxValue;
            ThrowMsgIsNull();
        }
    }

    private async Task ProcessOutboxMessageScoped(object outboxMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handler = handlerFactory.GetMessageHandler(outboxMessage);
        var processMessageMethod = handler.GetType().GetMethods().First(m => m is {Name: "Process"});
        if (processMessageMethod == null)
        {
            throw new OutboxException($"Can't find Process method for handler type '{handler}'");
        }

        try
        {
            var task = (Task?)processMessageMethod.Invoke(handler, new[] { outboxMessage, cancellationToken });
            await task!.ConfigureAwait(false);
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
    private static void ThrowMsgIsNull()
    {
        throw new OutboxException("Message is null");
    }
}