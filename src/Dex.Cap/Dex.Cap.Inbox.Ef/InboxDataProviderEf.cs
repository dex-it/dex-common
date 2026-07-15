using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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
    private EfTransactionOptions? _transactionOptions;

    public override async Task<InboxEnqueueStatus> Add(InboxEnvelope inboxEnvelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inboxEnvelope);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureNpgsql();
        EnsureNoEnclosingTransaction();

        // Дедупликацию решает уникальный индекс, а не предварительная проверка: SELECT-затем-INSERT
        // даёт гонку между конкурентными доставками одного сообщения. ON CONFLICT DO NOTHING
        // атомарен и делает дубль штатным исходом, а не исключением.
        var sql = $"""
                   INSERT INTO {NameConst.SchemaName}.{NameConst.TableName}
                       ("Id", "MessageId", "ConsumerId", "MessageType", "Content", "ActivityId",
                        "Retries", "Status", "CreatedUtc", "StartAtUtc", "ScheduledStartIndexing", "LockTimeout")
                   VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)
                   ON CONFLICT ("MessageId", "ConsumerId") DO NOTHING
                   """;

        // Параметры создаём через провайдера: ExecuteSqlRawAsync не умеет выводить тип для null,
        // а ActivityId может быть пустым, если приём идёт вне активной трассы.
        using var command = dbContext.Database.GetDbConnection().CreateCommand();

        var parameters = new object[]
        {
            CreateParameter(command, "p0", inboxEnvelope.Id),
            CreateParameter(command, "p1", inboxEnvelope.MessageId),
            CreateParameter(command, "p2", inboxEnvelope.ConsumerId),
            CreateParameter(command, "p3", inboxEnvelope.MessageType),
            CreateParameter(command, "p4", inboxEnvelope.Content),
            CreateParameter(command, "p5", inboxEnvelope.ActivityId, System.Data.DbType.String),
            CreateParameter(command, "p6", inboxEnvelope.Retries),
            CreateParameter(command, "p7", (int)inboxEnvelope.Status),
            CreateParameter(command, "p8", inboxEnvelope.CreatedUtc),
            // Оба поля заполняет конструктор InboxEnvelope, у принимаемого сообщения они всегда есть.
            CreateParameter(command, "p9", inboxEnvelope.StartAtUtc!.Value),
            CreateParameter(command, "p10", inboxEnvelope.ScheduledStartIndexing!.Value),
            CreateParameter(command, "p11", inboxEnvelope.LockTimeout)
        };

        var affected = await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken).ConfigureAwait(false);

        if (affected != 0)
        {
            return InboxEnqueueStatus.Accepted;
        }

        MetricCollector.IncDuplicateCount();
        logger.LogDebug(
            "Duplicate inbox message {MessageId} for consumer {ConsumerId} is ignored",
            inboxEnvelope.MessageId, inboxEnvelope.ConsumerId);

        return InboxEnqueueStatus.Duplicate;
    }

    /// <exception cref="OperationCanceledException"/>
    public override async Task<IInboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken)
    {
        var envelopes = await GetFreeMessages(cancellationToken).ConfigureAwait(false);

        // Непригодные строки отсеиваются ДО сборки задач. Иначе одна такая строка роняла бы сборку всей
        // партии, а её сообщения уже захвачены арендой: инбокс вставал бы навсегда, отдавая наружу
        // только LogCritical раз в цикл, и на следующем цикле повторял бы то же самое.
        var poisoned = envelopes.Where(x => x.LockTimeout < InboxEnvelope.MinLockTimeout).ToArray();

        if (poisoned.Length is not 0)
        {
            await DeadLetterPoisoned(poisoned, cancellationToken).ConfigureAwait(false);
        }

        return CreateJobs(envelopes.Where(x => x.LockTimeout >= InboxEnvelope.MinLockTimeout));
    }

    /// <summary>
    /// Похоронить строки, которые невозможно взять в обработку.
    /// </summary>
    /// <remarks>
    /// Единственная причина сюда попасть это LockTimeout ниже минимума. Конструктор конверта проверяет
    /// минимум, у колонки есть дефолт, но сущность публична и её свойства изменяемы, поэтому значение
    /// может приехать и через обычный EF-код потребителя, и правкой руками. Само не починится, поэтому
    /// пропускать строку из цикла в цикл означало бы вечно тратить на неё захват.
    /// Хороним явно, чтобы её увидели при разборе, а не искали причину простоя по логам.
    /// </remarks>
    private async Task DeadLetterPoisoned(InboxEnvelope[] poisoned, CancellationToken cancellationToken)
    {
        var ids = poisoned.Select(x => x.Id).ToArray();

        foreach (var envelope in poisoned)
        {
            logger.LogError(
                "Inbox message {MessageId} has LockTimeout {LockTimeout} below the minimum of {MinLockTimeout} and is dead lettered. " +
                "The value can only come from a write that bypassed the envelope constructor",
                envelope.Id, envelope.LockTimeout, InboxEnvelope.MinLockTimeout);
        }

        // Предикат повторяет само условие непригодности, а не только Id. Так метод самоперепроверяется:
        // если строку уже похоронил другой инстанс, а оператор успел починить LockTimeout и вернуть её
        // в обработку задокументированным сбросом, запоздалый вызов не перехоронит здоровую строку
        // с уже неверной причиной.
        var affected = await dbContext
            .Set<InboxEnvelope>()
            .Where(x => ids.Contains(x.Id) && x.LockTimeout < InboxEnvelope.MinLockTimeout)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, InboxMessageStatus.DeadLettered)
                    .SetProperty(x => x.ScheduledStartIndexing, (DateTime?)null)
                    .SetProperty(x => x.Updated, DateTime.UtcNow)
                    .SetProperty(x => x.LockId, (Guid?)null)
                    .SetProperty(x => x.ErrorMessage, $"LockTimeout is below the minimum of {InboxEnvelope.MinLockTimeout}"),
                cancellationToken)
            .ConfigureAwait(false);

        // Считаем фактически похороненные строки, а не намерение: строку мог похоронить другой инстанс.
        for (var i = 0; i < affected; i++)
        {
            MetricCollector.IncDeadLetteredCount();
        }
    }

    /// <summary>
    /// Собрать задачи, не оставив висящих таймеров, если сборка всё же прервётся.
    /// </summary>
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
            // Каждая уже созданная задача держит взведённый таймер отмены. Раскрутка стека их не соберёт,
            // а партия с непригодной строкой захватывается снова и снова, поэтому течь будет бесконечно.
            foreach (var job in jobs)
            {
                job.Dispose();
            }

            throw;
        }
    }

    public override int GetFreeMessagesCount()
    {
        // Фильтр по поддерживаемым дискриминаторам обязателен: без него глубина включала бы сообщения
        // чужих потребителей из той же таблицы, которые этот сервис не заберёт никогда, и алерт на
        // глубину очереди залипал бы навсегда. Тот же фильтр стоит в выборке.
        var supported = discriminatorProvider.SupportedDiscriminators;

        if (supported.Count is 0)
        {
            return 0;
        }

        return dbContext
            .Set<InboxEnvelope>()
            .Count(x => x.ScheduledStartIndexing != null && supported.Contains(x.MessageType));
    }

    public override int GetDeadLetteredMessagesCount()
    {
        // Тот же фильтр, что и у глубины очереди, и по той же причине: чужие похороненные сообщения
        // разбирать не этому сервису, и включать их в свою метрику означало бы залипший алерт.
        var supported = discriminatorProvider.SupportedDiscriminators;

        if (supported.Count is 0)
        {
            return 0;
        }

        return dbContext
            .Set<InboxEnvelope>()
            .Count(x => x.Status == InboxMessageStatus.DeadLettered && supported.Contains(x.MessageType));
    }

    private Task<InboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
    {
        _transactionOptions ??= new EfTransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            TimeoutInSeconds = (uint)Options.GetFreeMessagesTimeout.TotalSeconds
        };

        var lockId = Guid.NewGuid(); // Ключ идемпотентности.
        return dbContext.ExecuteInTransactionAsync(
            new { LockId = lockId, DbContext = dbContext },
            async (state, token) =>
            {
                var discriminators = discriminatorProvider.SupportedDiscriminators;

                // Ни одного обработчика в этом сервисе: брать нечего, и незачем ходить в БД.
                if (discriminators.Count is 0)
                    return [];

                var sql = GenerateFetchPlatformSpecificSql();

                var lockedEnvelopes = await state.DbContext
                    .Set<InboxEnvelope>()
                    .FromSqlRaw(sql, discriminators.ToArray(), state.LockId)
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken: token)
                    .ConfigureAwait(false);

                return lockedEnvelopes;
            },
            async (state, token) => await state.DbContext.Set<InboxEnvelope>()
                .AnyAsync(x => x.LockId == state.LockId, token).ConfigureAwait(false),
            _transactionOptions,
            cancellationToken: cancellationToken);
    }

    protected override Task CompleteJobAsync(IInboxLockedJob lockedJob, bool requireLease, CancellationToken cancellationToken)
    {
        // ReadCommitted, а не RepeatableRead: корректность обеспечивает предикат владения арендой
        // прямо в UPDATE, а не уровень изоляции. Более строгий уровень здесь запрещал бы
        // переиспользование этого метода внутри транзакции обработчика (Common.Ef не позволяет
        // повышать уровень у вложенной транзакции), а именно там фиксируется успех.
        return dbContext.ExecuteInTransactionAsync(
            (DbContext: dbContext, LockedJob: lockedJob, Logger: logger, RequireLease: requireLease),
            static async (state, ct) =>
            {
                var (context, job, log, requireLease) = state;
                var envelope = job.Envelope;

                // ExecuteUpdate, а не загрузка сущности с последующим SaveChanges: одиночный UPDATE
                // с предикатом владения атомарен и, что важнее, не трогает ChangeTracker — этот метод
                // вызывается внутри транзакции обработчика, чьи несохранённые изменения ронять нельзя.
                var affected = await context
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
                        ct)
                    .ConfigureAwait(false);

                if (affected != 0)
                {
                    return;
                }

                // Аренда истекла или её перехватил другой обработчик.
                if (requireLease)
                {
                    // Путь успеха: этот вызов идёт внутри транзакции обработчика. Вернуться нормально
                    // означало бы закоммитить изменения обработчика со старым статусом сообщения, и тогда
                    // следующий владелец аренды применил бы эффект второй раз. Исключение откатывает
                    // транзакцию, поэтому эффект применит ровно тот, кто владеет арендой.
                    throw new InboxLeaseLostException(
                        $"Inbox job {envelope.Id} lost its lease before the outcome was committed. " +
                        "The handler transaction is rolled back. " +
                        "Increase LockTimeout above the time needed to drain the whole claimed batch");
                }

                // Путь неудачи: транзакция обработчика уже откачена, ронять нечего. Сообщение остаётся
                // за новым владельцем аренды, он его и обработает.
                log.LogWarning(
                    "Inbox job {JobId} can not be completed: the lease has expired and the message was taken by another handler. " +
                    "Increase LockTimeout above the time needed to drain the whole claimed batch",
                    envelope.Id);
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

    private void EnsureNpgsql()
    {
        var providerName = dbContext.Database.ProviderName;

        if (providerName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
            throw new NotSupportedException($"The provider {providerName} is not supported.");
    }

    /// <summary>
    /// Убедиться, что приём не выполняется внутри чужой транзакции.
    /// </summary>
    /// <remarks>
    /// Инбокс обязан зафиксировать сообщение ДО того, как источнику уйдёт подтверждение. Внутри чужой
    /// транзакции этой гарантии нет: INSERT заэнлистится в неё, и на откате сообщение исчезнет, хотя
    /// EnqueueAsync уже вернул Accepted и источник получил ack. Падаем явно, потому что тихо
    /// отработать «как получится» здесь означает молча терять сообщения.
    /// </remarks>
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

    /// <summary>
    /// Собрать SQL захвата партии.
    /// </summary>
    /// <remarks>
    /// Дискриминаторы и ключ аренды передаются ПАРАМЕТРАМИ ({0} и {1} подставляет EF), а не подстановкой
    /// в текст запроса. Подстановка требовала бы ограничивать набор символов дискриминатора: кавычка
    /// сломала бы литерал на стороне Postgres, а фигурная скобка ещё раньше, на string.Format внутри EF.
    /// Ограничивать пришлось бы с запасом, отвергая заведомо безопасные значения вроде MessageUrn
    /// MassTransit ('urn:message:...') или имени вложенного типа ('Outer+Inner'). Параметр снимает
    /// вопрос целиком: значение не попадает в текст запроса вообще.
    /// <para>
    /// LIMIT намеренно остаётся литералом: планировщику нужно знать его на этапе построения плана, чтобы
    /// оборвать упорядоченный обход индекса на первых MessagesToProcess строках. Значение это int,
    /// проверенный валидатором опций на старте.
    /// </para>
    /// </remarks>
    private string GenerateFetchPlatformSpecificSql()
    {
        EnsureNpgsql();

        const string cScheduledStartIndexing = nameof(InboxEnvelope.ScheduledStartIndexing);
        const string cStatus = nameof(InboxEnvelope.Status);
        const string cLockId = nameof(InboxEnvelope.LockId);
        const string cLockExpirationTimeUtc = nameof(InboxEnvelope.LockExpirationTimeUtc);
        const string cLockTimeout = nameof(InboxEnvelope.LockTimeout);
        const string cStartAtUtc = nameof(InboxEnvelope.StartAtUtc);
        const string cMessageType = nameof(InboxEnvelope.MessageType);

        // FOR UPDATE SKIP LOCKED + простановка аренды одним стейтментом: конкурентные инстансы
        // не могут захватить одну и ту же строку, поэтому сервис масштабируется горизонтально.
        // Статус DeadLettered и Succeeded в выборку не попадают: у них ScheduledStartIndexing IS NULL.
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
}
