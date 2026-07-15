using System;
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
    /// <summary>
    /// Обработать одно захваченное сообщение и зафиксировать исход.
    /// </summary>
    /// <remarks>
    /// Любая ошибка обработки это исход сообщения, а не авария процесса: битое тело, неизвестный
    /// дискриминатор и падение самого обработчика уводят сообщение в повтор, а по исчерпании попыток в
    /// dead letter, но хост продолжает работать.
    /// </remarks>
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
            await HandleExpiredLease(job).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            HandleHostStopping(job);
            throw;
        }
        catch (InboxLeaseLostException ex)
        {
            HandleLeaseTakenOver(job, ex);
        }
        catch (Exception ex)
        {
            await HandleProcessingFailure(job, ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Аренда истекла прямо во время обработки.
    /// </summary>
    /// <remarks>
    /// Неудача фиксируется вне отменённого токена, иначе исход потерялся бы. Строку мог уже перехватить
    /// другой обработчик: тогда фиксация ничего не изменит и ограничится предупреждением.
    /// </remarks>
    private async Task HandleExpiredLease(IInboxLockedJob job)
    {
        logger.LogError(
            "Operation canceled due to exceeding the message blocking time. MessageId: {MessageId}",
            job.Envelope.Id);

        DiscardRolledBackChanges();

        await dataProvider.JobFail(job, "Lock is expired", cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Штатная остановка хоста.
    /// </summary>
    /// <remarks>
    /// Сообщение не помечается неудачным и попытка не тратится: транзакция откатилась, аренда истечёт
    /// сама, и сообщение будет обработано заново без штрафа.
    /// </remarks>
    private void HandleHostStopping(IInboxLockedJob job) =>
        logger.LogDebug("Operation canceled due to host stopping. MessageId: {MessageId}", job.Envelope.Id);

    /// <summary>
    /// Аренду перехватили до фиксации успеха.
    /// </summary>
    /// <remarks>
    /// Транзакция обработчика откачена, эффект не применён, поэтому попытка не тратится и в строку ничего
    /// не пишется: ею уже владеет другой обработчик, он её и закроет.
    /// </remarks>
    private void HandleLeaseTakenOver(IInboxLockedJob job, InboxLeaseLostException exception)
    {
        logger.LogWarning(exception,
            "Inbox message {MessageId} lost its lease before completion, leaving it to the new owner",
            job.Envelope.Id);

        DiscardRolledBackChanges();
    }

    /// <summary>
    /// Обработка завершилась ошибкой: сообщение уходит в повтор.
    /// </summary>
    private async Task HandleProcessingFailure(IInboxLockedJob job, Exception exception)
    {
        logger.LogError(exception, "Failed to process inbox message {MessageId}", job.Envelope.Id);

        DiscardRolledBackChanges();

        await dataProvider.JobFail(job, exception.Message, exception, CancellationToken.None).ConfigureAwait(false);
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
    /// Атомарность здесь суть паттерна: если бизнес-эффект закоммитится, а статус нет, сообщение будет
    /// обработано повторно и эффект применится дважды. Поэтому фиксация успеха идёт внутри той же
    /// транзакции, что и изменения обработчика.
    /// <para>
    /// Проверка идемпотентности нужна ретраю EF ExecutionStrategy: если транзакция всё же закоммитилась,
    /// повторять обработку нельзя.
    /// </para>
    /// </remarks>
    private Task ProcessJobCore(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        return dbContext.ExecuteInTransactionAsync(
            job,
            async (state, token) =>
            {
                await InvokeHandler(state, token).ConfigureAwait(false);
                await dataProvider.JobSucceed(state, token).ConfigureAwait(false);
            },
            async (state, token) => await dbContext
                .Set<InboxEnvelope>()
                .AnyAsync(x => x.Id == state.Envelope.Id && x.Status == InboxMessageStatus.Succeeded, token)
                .ConfigureAwait(false),
            EfTransactionOptions.Default,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Восстановить сообщение из конверта и передать его обработчику.
    /// </summary>
    /// <remarks>
    /// Вызов идёт через контракт интерфейса, а не поиском метода Process рефлексией. Поиск по имени
    /// выбирал произвольный метод у класса, обрабатывающего несколько типов сообщений, не находил явную
    /// реализацию интерфейса (она компилируется в приватный метод) и заворачивал синхронно брошенные
    /// исключения в TargetInvocationException, из-за чего фильтры отмены выше по стеку не срабатывали.
    /// <para>
    /// Обработчик не диспозится: он получен из DI, его время жизни принадлежит scope этой задачи, и scope
    /// закроет его сам. Диспоз чужого объекта сломал бы обработчик, зарегистрированный синглтоном.
    /// </para>
    /// </remarks>
    /// <exception cref="DiscriminatorResolveException">Тип сообщения неизвестен этому сервису.</exception>
    /// <exception cref="InboxException">Тело сообщения не восстанавливается.</exception>
    private Task InvokeHandler(IInboxLockedJob job, CancellationToken cancellationToken)
    {
        var envelope = job.Envelope;

        if (!discriminatorProvider.CurrentDomainInboxMessageTypes.TryGetValue(envelope.MessageType, out var messageType))
        {
            throw new DiscriminatorResolveException($"Can't find Type for discriminator - {envelope.MessageType}.");
        }

        var message = serializer.Deserialize(messageType, envelope.Content)
                      ?? throw new InboxException($"Message is null after deserialization, discriminator - {envelope.MessageType}");

        var handler = handlerFactory.GetMessageHandler(messageType);

        return handlerFactory.GetInvoker(messageType).InvokeAsync(handler, message, cancellationToken);
    }
}
