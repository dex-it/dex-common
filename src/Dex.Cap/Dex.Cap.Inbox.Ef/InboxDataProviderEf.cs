using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Jobs;
using Dex.Cap.Inbox.Models;
using Dex.Cap.Inbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxDataProviderEf<TDbContext>(
    TDbContext dbContext,
    IOptions<InboxOptions> inboxOptions,
    ILogger<InboxDataProviderEf<TDbContext>> logger,
    IInboxRetryStrategy retryStrategy,
    IInboxMetricCollector metricCollector,
    IInboxTypeDiscriminatorProvider discriminatorProvider)
    : BaseInboxDataProvider(retryStrategy, inboxOptions, metricCollector)
    where TDbContext : DbContext
{
    /// <remarks>
    /// Дедупликацию решает уникальный индекс, а не предварительная проверка: SELECT-затем-INSERT даёт
    /// гонку между конкурентными доставками одного сообщения. ON CONFLICT DO NOTHING атомарен и делает
    /// повтор штатным исходом, а не исключением.
    /// <para>
    /// Вставка НЕ повторяется при транзиентном отказе, и это не упущение с двух сторон сразу. Со стороны EF:
    /// <c>ExecuteSqlRawAsync</c> намеренно не использует стратегию повторов («the current ExecutionStrategy is
    /// not used by this method since the SQL may not be idempotent and does not run in a transaction»), то есть
    /// настроенный потребителем EnableRetryOnFailure сюда не распространяется. Со стороны инбокса повторять и
    /// незачем: подтверждение источнику отдаётся только после успешной вставки, поэтому отказ обязан дойти до
    /// вызывающего, а роль повтора играет редоставка источника. Внутренний повтор дублировал бы её и при этом
    /// возвращал бы <see cref="InboxEnqueueStatus.Duplicate"/> на собственную строку.
    /// </para>
    /// </remarks>
    private const string InsertSql = $"""
                                      INSERT INTO {NameConst.SchemaName}.{NameConst.TableName}
                                          ("Id", "MessageId", "ConsumerId", "MessageType", "Content", "ActivityId",
                                           "Retries", "Status", "CreatedUtc", "StartAtUtc", "ScheduledStartIndexing", "LockTimeout")
                                      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)
                                      ON CONFLICT ("MessageId", "ConsumerId") DO NOTHING
                                      """;

    private EfTransactionOptions? _transactionOptions;

    /// <inheritdoc />
    public override async Task<InboxEnqueueStatus> Add(InboxEnvelope inboxEnvelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inboxEnvelope);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.Database.EnsureNpgsql();
        EnsureNoEnclosingTransaction();

        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        var parameters = CreateEnqueueParameters(command, inboxEnvelope);

        var affected = await dbContext.Database
            .ExecuteSqlRawAsync(InsertSql, parameters, cancellationToken)
            .ConfigureAwait(false);

        return affected is not 0
            ? InboxEnqueueStatus.Accepted
            : ReportDuplicate(inboxEnvelope);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Непригодные строки отсеиваются ДО сборки задач. Иначе одна такая строка роняла бы сборку всей
    /// партии, сообщения которой уже захвачены арендой: инбокс вставал бы навсегда, отдавая наружу
    /// только LogCritical раз в цикл и повторяя то же самое на следующем.
    /// </remarks>
    /// <exception cref="OperationCanceledException"/>
    public override async Task<InboxJobBatch> GetWaitingJobs(CancellationToken cancellationToken)
    {
        var envelopes = await GetFreeMessages(cancellationToken).ConfigureAwait(false);
        var poisoned = envelopes.Where(IsPoisoned).ToArray();

        if (poisoned.Length is not 0)
        {
            await DeadLetterPoisoned(poisoned, cancellationToken).ConfigureAwait(false);
        }

        var jobs = CreateJobs(envelopes.Where(x => !IsPoisoned(x)));

        // ClaimedCount это все захваченные строки, включая похороненные непригодные: по нему планировщик судит
        // о полноте партии, а не по числу дошедших до обработчика задач.
        return new InboxJobBatch(jobs, envelopes.Length);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Считаются только сообщения этого сервиса. Без фильтра глубина включала бы сообщения чужих
    /// потребителей из той же таблицы, которые этот сервис не заберёт никогда, и алерт на глубину
    /// очереди залипал бы навсегда. Тот же фильтр стоит в выборке и в чистке.
    /// </remarks>
    public override int GetFreeMessagesCount()
    {
        var own = discriminatorProvider.SupportedDiscriminators;

        if (own.Count is 0)
        {
            return 0;
        }

        return dbContext
            .Set<InboxEnvelope>()
            .Count(x => x.ScheduledStartIndexing != null && own.Contains(x.MessageType));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Фильтр по своим дискриминаторам обязателен по той же причине, что и у глубины очереди: чужие
    /// похороненные сообщения разбирать не этому сервису, и включать их в свою метрику означало бы
    /// залипший алерт.
    /// </remarks>
    public override int GetDeadLetteredMessagesCount()
    {
        var own = discriminatorProvider.SupportedDiscriminators;

        if (own.Count is 0)
        {
            return 0;
        }

        return dbContext
            .Set<InboxEnvelope>()
            .Count(x => x.Status == InboxMessageStatus.DeadLettered && own.Contains(x.MessageType));
    }

    /// <inheritdoc />
    public override Task<int> RequeueDeadLetteredAsync(InboxMessageIdentity identity, CancellationToken cancellationToken)
    {
        var own = discriminatorProvider.SupportedDiscriminators;

        if (own.Count is 0)
        {
            return Task.FromResult(0);
        }

        return RequeueMatchingAsync(
            x => x.MessageId == identity.MessageId
                 && x.ConsumerId == identity.ConsumerId
                 && x.Status == InboxMessageStatus.DeadLettered
                 && own.Contains(x.MessageType),
            cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> RequeueAllDeadLetteredAsync(CancellationToken cancellationToken)
    {
        var own = discriminatorProvider.SupportedDiscriminators;

        if (own.Count is 0)
        {
            return Task.FromResult(0);
        }

        return RequeueMatchingAsync(
            x => x.Status == InboxMessageStatus.DeadLettered && own.Contains(x.MessageType),
            cancellationToken);
    }

    /// <summary>
    /// Сбросить состояние отказа так, чтобы строку снова забрала выборка.
    /// </summary>
    /// <remarks>
    /// Обычный EF ExecuteUpdate без сырого SQL, поэтому перенесётся на любой EF-провайдер. Идемпотентен:
    /// предикат требует статус DeadLettered, поэтому повторный вызов по уже возвращённой строке её не
    /// находит и ничего не делает. Аренда снимается на случай строки, похороненной в обход штатного пути.
    /// </remarks>
    private Task<int> RequeueMatchingAsync(
        Expression<Func<InboxEnvelope, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return dbContext
            .Set<InboxEnvelope>()
            .Where(predicate)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.New)
                    .SetProperty(x => x.Retries, 0)
                    .SetProperty(x => x.StartAtUtc, now)
                    .SetProperty(x => x.ScheduledStartIndexing, now)
                    .SetProperty(x => x.ErrorMessage, (string?)null)
                    .SetProperty(x => x.Error, (string?)null)
                    .SetProperty(x => x.LockId, (Guid?)null)
                    .SetProperty(x => x.LockExpirationTimeUtc, (DateTime?)null)
                    .SetProperty(x => x.Updated, now),
                cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task WriteOutcomeOrThrowAsync(IInboxLockedJob lockedJob, CancellationToken cancellationToken)
    {
        var written = await CompleteJobAsync(lockedJob, cancellationToken).ConfigureAwait(false);

        if (!written)
        {
            throw new InboxLeaseLostException(
                $"Inbox job {lockedJob.Envelope.Id} lost its lease before the outcome was committed. " +
                $"The handler transaction is rolled back. {LeaseAdvice}");
        }
    }

    /// <inheritdoc />
    protected override Task<bool> TryWriteOutcomeAsync(IInboxLockedJob lockedJob, CancellationToken cancellationToken) =>
        CompleteJobAsync(lockedJob, cancellationToken);

    /// <summary>
    /// Записать исход одной транзакцией и сообщить, состоялась ли запись.
    /// </summary>
    /// <remarks>
    /// Уровень изоляции ReadCommitted, а не RepeatableRead: корректность обеспечивает предикат владения
    /// арендой прямо в UPDATE, а не уровень изоляции. Более строгий уровень запрещал бы вызывать этот
    /// метод внутри транзакции обработчика (Common.Ef не позволяет повышать уровень у вложенной
    /// транзакции), а именно там фиксируется успех.
    /// </remarks>
    private Task<bool> CompleteJobAsync(IInboxLockedJob lockedJob, CancellationToken cancellationToken)
    {
        return dbContext.ExecuteInTransactionAsync(
            (DbContext: dbContext, LockedJob: lockedJob, Logger: logger, Metrics: MetricCollector),
            static async (state, ct) =>
            {
                var (context, job, log, metrics) = state;
                var affected = await WriteOutcomeAsync(context, job, ct).ConfigureAwait(false);

                if (affected is 0)
                {
                    ReportLostLease(job.Envelope, log, metrics);
                }

                return affected is not 0;
            },
            static async (state, ct) =>
            {
                var (context, job, _, _) = state;
                var envelope = job.Envelope;

                var existLocked = await context.Set<InboxEnvelope>()
                    .AnyAsync(x => x.Id == envelope.Id && x.LockId == job.LockId, ct)
                    .ConfigureAwait(false);

                return !existLocked;
            },
            EfTransactionOptions.Default,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Записать исход обработки, если аренда всё ещё принадлежит этому обработчику.
    /// </summary>
    /// <remarks>
    /// ExecuteUpdate, а не загрузка сущности с последующим SaveChanges: одиночный UPDATE с предикатом
    /// владения атомарен и, что важнее, не трогает ChangeTracker. Метод вызывается внутри транзакции
    /// обработчика, чьи несохранённые изменения ронять нельзя.
    /// </remarks>
    /// <returns>Количество обновлённых строк; ноль означает, что аренда потеряна.</returns>
    private static async Task<int> WriteOutcomeAsync(DbContext context, IInboxLockedJob job, CancellationToken cancellationToken)
    {
        var envelope = job.Envelope;

        return await context
            .Set<InboxEnvelope>()
            .Where(x => x.Id == envelope.Id
                        && x.LockId == job.LockId
                        && (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow))
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, envelope.Status)
                    .SetProperty(x => x.Updated, envelope.Updated)
                    .SetProperty(x => x.Retries, envelope.Retries)
                    .SetProperty(x => x.ErrorMessage, envelope.ErrorMessage)
                    .SetProperty(x => x.Error, envelope.Error)
                    .SetProperty(x => x.StartAtUtc, envelope.StartAtUtc)
                    .SetProperty(x => x.ScheduledStartIndexing, envelope.ScheduledStartIndexing)
                    .SetProperty(x => x.LockId, (Guid?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Совет, который одинаково полезен и в логе, и в тексте исключения.</summary>
    private const string LeaseAdvice = "Increase LockTimeout above the time needed to drain the whole claimed batch";

    /// <summary>
    /// Зафиксировать потерю аренды: она истекла или её перехватил другой обработчик.
    /// </summary>
    /// <remarks>
    /// Единственная точка обнаружения потери, поэтому счётчик живёт здесь: иначе каждый вызывающий считал
    /// бы её сам и рано или поздно забыл.
    /// </remarks>
    private static void ReportLostLease(InboxEnvelope envelope, ILogger logger, IInboxMetricCollector metrics)
    {
        metrics.IncLeaseLostCount();

        logger.LogWarning(
            "Inbox job {JobId} can not be completed: the lease has expired and the message was taken by another handler. " + LeaseAdvice,
            envelope.Id);
    }

    /// <summary>
    /// Строку невозможно взять в обработку.
    /// </summary>
    /// <remarks>
    /// Причина всегда одна: LockTimeout вне допустимого диапазона, из-за чего таймер отмены задачи либо не
    /// оставил бы обработчику окна вовсе, либо не принял бы такой интервал и бросил. Конструктор конверта
    /// проверяет обе границы, у колонки есть дефолт, но сущность публична и её свойства изменяемы, поэтому
    /// значение может приехать и через обычный EF-код потребителя, и правкой руками.
    /// </remarks>
    private static bool IsPoisoned(InboxEnvelope envelope) =>
        envelope.LockTimeout < InboxEnvelope.MinLockTimeout || envelope.LockTimeout > InboxEnvelope.MaxLockTimeout;

    /// <summary>
    /// Похоронить строки, которые невозможно взять в обработку.
    /// </summary>
    /// <remarks>
    /// Непригодность сама не починится, поэтому пропускать такую строку из цикла в цикл означало бы вечно
    /// тратить на неё захват. Хороним явно, чтобы её увидели при разборе, а не искали причину простоя по
    /// логам.
    /// <para>
    /// Предикат повторяет само условие непригодности, а не только Id: если строку уже похоронил другой
    /// инстанс, а оператор успел починить LockTimeout и вернуть её в обработку задокументированным
    /// сбросом, запоздалый вызов не перехоронит здоровую строку с уже неверной причиной.
    /// </para>
    /// </remarks>
    private async Task DeadLetterPoisoned(InboxEnvelope[] poisoned, CancellationToken cancellationToken)
    {
        var ids = poisoned.Select(x => x.Id).ToArray();

        foreach (var envelope in poisoned)
        {
            logger.LogError(
                "Inbox message {MessageId} has LockTimeout {LockTimeout} outside the allowed range " +
                "[{MinLockTimeout}, {MaxLockTimeout}] and is dead lettered. The value can only come from a write " +
                "that bypassed the envelope constructor",
                envelope.Id, envelope.LockTimeout, InboxEnvelope.MinLockTimeout, InboxEnvelope.MaxLockTimeout);
        }

        var affected = await dbContext
            .Set<InboxEnvelope>()
            .Where(x => ids.Contains(x.Id)
                        && (x.LockTimeout < InboxEnvelope.MinLockTimeout || x.LockTimeout > InboxEnvelope.MaxLockTimeout))
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.DeadLettered)
                    .SetProperty(x => x.ScheduledStartIndexing, (DateTime?)null)
                    .SetProperty(x => x.Updated, DateTime.UtcNow)
                    .SetProperty(x => x.LockId, (Guid?)null)
                    .SetProperty(x => x.ErrorMessage,
                        $"LockTimeout is outside the allowed range [{InboxEnvelope.MinLockTimeout}, {InboxEnvelope.MaxLockTimeout}]"),
                cancellationToken)
            .ConfigureAwait(false);

        CountDeadLettered(affected);
    }

    /// <summary>
    /// Учесть фактически похороненные строки, а не намерение: строку мог похоронить другой инстанс.
    /// </summary>
    private void CountDeadLettered(int affected)
    {
        for (var i = 0; i < affected; i++)
        {
            MetricCollector.IncDeadLetteredCount();
        }
    }

    /// <summary>
    /// Собрать задачи, не оставив висящих таймеров, если сборка всё же прервётся.
    /// </summary>
    /// <remarks>
    /// Каждая созданная задача держит взведённый таймер отмены. Раскрутка стека их не соберёт, а партия
    /// с непригодной строкой захватывается снова и снова, поэтому течь будет бесконечно.
    /// </remarks>
    private static IInboxLockedJob[] CreateJobs(IEnumerable<InboxEnvelope> envelopes)
    {
        var jobs = new List<IInboxLockedJob>();

        try
        {
            foreach (var envelope in envelopes)
            {
                jobs.Add(new InboxLockedJob(envelope));
            }

            return jobs.ToArray();
        }
        catch
        {
            foreach (var job in jobs)
            {
                job.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// Захватить партию свободных сообщений, проставив им аренду.
    /// </summary>
    private Task<InboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
    {
        _transactionOptions ??= new EfTransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            TimeoutInSeconds = (uint)Options.GetFreeMessagesTimeout.TotalSeconds
        };

        return dbContext.ExecuteInTransactionAsync(
            new { LockId = Guid.NewGuid(), DbContext = dbContext },
            async (state, token) =>
            {
                var own = discriminatorProvider.SupportedDiscriminators;

                if (own.Count is 0)
                {
                    return [];
                }

                return await state.DbContext
                    .Set<InboxEnvelope>()
                    .FromSqlRaw(GenerateFetchPlatformSpecificSql(), own.ToArray(), state.LockId)
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken: token)
                    .ConfigureAwait(false);
            },
            async (state, token) => await state.DbContext.Set<InboxEnvelope>()
                .AnyAsync(x => x.LockId == state.LockId, token).ConfigureAwait(false),
            _transactionOptions,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Собрать SQL захвата партии.
    /// </summary>
    /// <remarks>
    /// FOR UPDATE SKIP LOCKED и простановка аренды идут одним стейтментом, поэтому конкурентные инстансы
    /// не могут захватить одну и ту же строку и сервис масштабируется горизонтально. Сообщения в статусах
    /// DeadLettered и Succeeded в выборку не попадают: у них ScheduledStartIndexing равен null.
    /// <para>
    /// Дискриминаторы и ключ аренды передаются ПАРАМЕТРАМИ ({0} и {1} подставляет EF), а не подстановкой
    /// в текст запроса. Подстановка требовала бы ограничивать набор символов дискриминатора: кавычка
    /// сломала бы литерал на стороне Postgres, а фигурная скобка ещё раньше, на string.Format внутри EF.
    /// Ограничивать пришлось бы с запасом, отвергая заведомо безопасные значения вроде MessageUrn
    /// MassTransit ('urn:message:...') или имени вложенного типа ('Outer+Inner').
    /// </para>
    /// <para>
    /// LIMIT намеренно остаётся литералом: планировщику нужно знать его на этапе построения плана, чтобы
    /// оборвать упорядоченный обход индекса на первых MessagesToProcess строках. Значение это int,
    /// проверенный валидатором опций на старте.
    /// </para>
    /// </remarks>
    private string GenerateFetchPlatformSpecificSql()
    {
        dbContext.Database.EnsureNpgsql();

        const string cScheduledStartIndexing = nameof(InboxEnvelope.ScheduledStartIndexing);
        const string cStatus = nameof(InboxEnvelope.Status);
        const string cLockId = nameof(InboxEnvelope.LockId);
        const string cLockExpirationTimeUtc = nameof(InboxEnvelope.LockExpirationTimeUtc);
        const string cLockTimeout = nameof(InboxEnvelope.LockTimeout);
        const string cStartAtUtc = nameof(InboxEnvelope.StartAtUtc);
        const string cMessageType = nameof(InboxEnvelope.MessageType);

        return $$"""
                 WITH cte AS (
                     SELECT "Id"
                     FROM {{NameConst.SchemaName}}.{{NameConst.TableName}}
                     WHERE "{{cScheduledStartIndexing}}" IS NOT NULL
                       AND "{{cMessageType}}" = ANY({0})
                       AND CURRENT_TIMESTAMP >= "{{cStartAtUtc}}"
                       AND ("{{cStatus}}" = {{InboxMessageStatus.New:D}} OR "{{cStatus}}" = {{InboxMessageStatus.Failed:D}})
                       AND ("{{cLockId}}" IS NULL OR "{{cLockExpirationTimeUtc}}" IS NULL OR "{{cLockExpirationTimeUtc}}" < CURRENT_TIMESTAMP)
                     ORDER BY "{{cScheduledStartIndexing}}"
                     LIMIT {{Options.MessagesToProcess}}
                     FOR UPDATE SKIP LOCKED
                 )
                 UPDATE {{NameConst.SchemaName}}.{{NameConst.TableName}} AS ie
                 SET
                     "{{cLockId}}" = {1},
                     "{{cLockExpirationTimeUtc}}" = CURRENT_TIMESTAMP + ie."{{cLockTimeout}}"
                 FROM cte
                 WHERE ie."Id" = cte."Id"
                 RETURNING ie.*;
                 """;
    }

    /// <summary>
    /// Зафиксировать повторную доставку: сообщение уже принято, сохранять нечего.
    /// </summary>
    private InboxEnqueueStatus ReportDuplicate(InboxEnvelope envelope)
    {
        MetricCollector.IncDuplicateCount();
        logger.LogDebug(
            "Duplicate inbox message {MessageId} for consumer {ConsumerId} is ignored",
            envelope.MessageId, envelope.ConsumerId);

        return InboxEnqueueStatus.Duplicate;
    }

    /// <summary>
    /// Параметры INSERT приёма.
    /// </summary>
    /// <remarks>
    /// Параметры создаются через провайдера, а не передаются значениями: ExecuteSqlRawAsync не умеет
    /// выводить тип для null, а ActivityId пуст, если приём идёт вне активной трассы.
    /// <see cref="InboxEnvelope.StartAtUtc"/> и <see cref="InboxEnvelope.ScheduledStartIndexing"/>
    /// заполняет конструктор конверта, поэтому у принимаемого сообщения они всегда есть.
    /// </remarks>
    private static object[] CreateEnqueueParameters(DbCommand command, InboxEnvelope envelope) =>
    [
        CreateParameter(command, "p0", envelope.Id),
        CreateParameter(command, "p1", envelope.MessageId),
        CreateParameter(command, "p2", envelope.ConsumerId),
        CreateParameter(command, "p3", envelope.MessageType),
        CreateParameter(command, "p4", envelope.Content),
        CreateParameter(command, "p5", envelope.ActivityId, System.Data.DbType.String),
        CreateParameter(command, "p6", envelope.Retries),
        CreateParameter(command, "p7", (int)envelope.Status),
        CreateParameter(command, "p8", envelope.CreatedUtc),
        CreateParameter(command, "p9", envelope.StartAtUtc!.Value),
        CreateParameter(command, "p10", envelope.ScheduledStartIndexing!.Value),
        CreateParameter(command, "p11", envelope.LockTimeout)
    ];

    private static DbParameter CreateParameter(DbCommand command, string name, object? value, System.Data.DbType? dbType = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;

        if (dbType.HasValue)
        {
            parameter.DbType = dbType.Value;
        }

        return parameter;
    }

    /// <summary>
    /// Убедиться, что приём не выполняется внутри чужой транзакции.
    /// </summary>
    /// <remarks>
    /// Инбокс обязан зафиксировать сообщение ДО того, как источнику уйдёт подтверждение. Внутри чужой
    /// транзакции этой гарантии нет: INSERT заэнлистится в неё, и на откате сообщение исчезнет, хотя
    /// EnqueueAsync уже вернул Accepted и источник получил ack. Падаем явно, потому что тихо отработать
    /// «как получится» здесь означает молча терять сообщения.
    /// </remarks>
    /// <exception cref="InboxException">Приём выполняется внутри транзакции вызывающего кода.</exception>
    private void EnsureNoEnclosingTransaction()
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            throw new InboxException(
                "Inbox message can not be enqueued inside an enclosing DbContext transaction: the message would be " +
                "committed or rolled back together with the caller, while the source has already been acknowledged. " +
                "Enqueue the message outside of the transaction.");
        }

        if (Transaction.Current is not null)
        {
            throw new InboxException(
                "Inbox message can not be enqueued inside an ambient TransactionScope: the message would be " +
                "committed or rolled back together with the caller, while the source has already been acknowledged. " +
                "Enqueue the message outside of the scope, or suppress it with TransactionScopeOption.Suppress.");
        }
    }
}