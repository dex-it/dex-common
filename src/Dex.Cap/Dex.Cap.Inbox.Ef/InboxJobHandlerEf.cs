using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Jobs;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxJobHandlerEf<TDbContext>(
    TDbContext dbContext,
    IInboxDataProvider dataProvider,
    IInboxSerializer serializer,
    IInboxMessageHandlerFactory handlerFactory,
    IInboxTypeDiscriminatorProvider discriminatorProvider,
    ILogger<InboxJobHandlerEf<TDbContext>> logger) : IInboxJobHandler
    where TDbContext : DbContext
{
    public async Task ProcessJob(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        logger.LogDebug("Message has been started to process {MessageId}", job.Envelope.Id);

        try
        {
            await ProcessJobCore(job, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (job.LockToken.IsCancellationRequested)
        {
            logger.LogError(
                "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}",
                job.Envelope.Id);

            DiscardRolledBackChanges();

            // Аренда истекла: фиксируем неудачу вне отменённого токена, иначе исход потеряется.
            // Строку мог уже перехватить другой обработчик — тогда CompleteJobAsync ничего не изменит и предупредит.
            await dataProvider.JobFail(job, "Lock is expired", cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Штатная остановка хоста. Не помечаем Failed и не тратим попытку: транзакция откатилась,
            // аренда истечёт сама, и сообщение будет обработано заново без штрафа.
            logger.LogDebug("Operation canceled due to host stopping. MessageId: {MessageId}", job.Envelope.Id);
            throw;
        }
        catch (InboxLeaseLostException ex)
        {
            // Аренду перехватили до фиксации успеха. Транзакция обработчика откачена, эффект не применён,
            // поэтому попытку не тратим и в строку не пишем: ею уже владеет другой обработчик, он её и закроет.
            logger.LogWarning(ex, "Inbox message {MessageId} lost its lease before completion, leaving it to the new owner", job.Envelope.Id);

            DiscardRolledBackChanges();
        }
        catch (Exception ex)
        {
            // Любая ошибка обработки — это исход сообщения, а не авария процесса. Сюда попадает и битое тело
            // (JsonException), и неизвестный дискриминатор, и падение обработчика: сообщение уйдёт в повтор,
            // а по исчерпании попыток в DeadLettered, но хост продолжит работать.
            logger.LogError(ex, "Failed to process inbox message {MessageId}", job.Envelope.Id);

            DiscardRolledBackChanges();

            await dataProvider.JobFail(job, ex.Message, ex, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Выбросить изменения обработчика, оставшиеся в ChangeTracker после отката транзакции.
    /// </summary>
    /// <remarks>
    /// Обработчик мог успеть добавить сущности до падения. Транзакция откатилась, и в БД их нет,
    /// но в ChangeTracker они остались: без очистки следующая транзакция (фиксация неудачи) упала бы
    /// на защите от несохранённых изменений, а сама неудача не записалась бы.
    /// <para>
    /// Чистка безопасна именно здесь: транзакция уже откачена, поэтому все отслеживаемые изменения
    /// заведомо устарели, а DbContext создаётся на каждое сообщение и ни с кем не разделяется.
    /// </para>
    /// </remarks>
    private void DiscardRolledBackChanges()
    {
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Выполнить обработчик и зафиксировать успех одной транзакцией.
    /// </summary>
    /// <remarks>
    /// Атомарность здесь — суть паттерна: если бизнес-эффект закоммитится, а статус нет,
    /// сообщение будет обработано повторно и эффект применится дважды.
    /// </remarks>
    private Task ProcessJobCore(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        return dbContext.ExecuteInTransactionAsync(
            job,
            async (state, token) =>
            {
                await InvokeHandler(state, token).ConfigureAwait(false);

                // Внутри той же транзакции: статус коммитится вместе с изменениями обработчика.
                await dataProvider.JobSucceed(state, token).ConfigureAwait(false);
            },
            // Идемпотентная проверка для ретрая EF ExecutionStrategy: если транзакция всё же закоммитилась,
            // повторять обработку нельзя.
            async (state, token) => await dbContext
                .Set<InboxEnvelope>()
                .AnyAsync(x => x.Id == state.Envelope.Id && x.Status == InboxMessageStatus.Succeeded, token)
                .ConfigureAwait(false),
            EfTransactionOptions.Default,
            cancellationToken: cancellationToken);
    }

    private async Task InvokeHandler(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        var envelope = job.Envelope;

        if (!discriminatorProvider.CurrentDomainInboxMessageTypes.TryGetValue(envelope.MessageType, out var messageType))
        {
            throw new DiscriminatorResolveException($"Can't find Type for discriminator - {envelope.MessageType}.");
        }

        var message = serializer.Deserialize(messageType, envelope.Content)
                      ?? throw new InboxException($"Message is null after deserialization, discriminator - {envelope.MessageType}");

        var handler = handlerFactory.GetMessageHandler(messageType);

        var processMethod = handler.GetType()
                                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(m => m.Name == nameof(IInboxMessageHandler<InboxMessageExample>.Process))
                            ?? throw new InboxException($"Can't find Process method for handler type '{handler.GetType()}'");

        try
        {
            var task = (Task)processMethod.Invoke(handler, [message, cancellationToken])!;
            await task.ConfigureAwait(false);
        }
        finally
        {
            if (handler is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Нужен только чтобы сослаться на имя метода через nameof: у IInboxMessage есть static abstract член,
    /// поэтому сам интерфейс нельзя использовать как аргумент типа.
    /// </summary>
    private abstract class InboxMessageExample : IInboxMessage
    {
        public static string InboxTypeId => string.Empty;
    }
}
