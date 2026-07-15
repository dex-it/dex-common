using System;
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

        return envelopes.Select(IInboxLockedJob (x) => new InboxLockedJob(x)).ToArray();
    }

    public override int GetFreeMessagesCount()
    {
        return dbContext
            .Set<InboxEnvelope>()
            .Count(x => x.ScheduledStartIndexing != null);
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
                var sql = GenerateFetchPlatformSpecificSql(state.LockId);

                if (sql is null)
                    return [];

                var lockedEnvelopes = await state.DbContext
                    .Set<InboxEnvelope>()
                    .FromSqlRaw(sql)
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

    private string? GenerateFetchPlatformSpecificSql(Guid lockId)
    {
        EnsureNpgsql();

        const string cScheduledStartIndexing = nameof(InboxEnvelope.ScheduledStartIndexing);
        const string cStatus = nameof(InboxEnvelope.Status);
        const string cLockId = nameof(InboxEnvelope.LockId);
        const string cLockExpirationTimeUtc = nameof(InboxEnvelope.LockExpirationTimeUtc);
        const string cLockTimeout = nameof(InboxEnvelope.LockTimeout);
        const string cStartAtUtc = nameof(InboxEnvelope.StartAtUtc);
        const string cMessageType = nameof(InboxEnvelope.MessageType);

        var discriminators = discriminatorProvider.SupportedDiscriminators;

        if (discriminators.Count is 0)
            return null;

        var discriminatorsSql = string.Join(", ", discriminators.Select(d => $"'{d}'"));

        // FOR UPDATE SKIP LOCKED + простановка аренды одним стейтментом: конкурентные инстансы
        // не могут захватить одну и ту же строку, поэтому сервис масштабируется горизонтально.
        // Статус DeadLettered и Succeeded в выборку не попадают: у них ScheduledStartIndexing IS NULL.
        return $"""
                WITH cte AS (
                    SELECT "Id"
                    FROM {NameConst.SchemaName}.{NameConst.TableName}
                    WHERE "{cScheduledStartIndexing}" IS NOT NULL
                      AND "{cMessageType}" IN ({discriminatorsSql})
                      AND CURRENT_TIMESTAMP >= "{cStartAtUtc}"
                      AND ("{cStatus}" = {InboxMessageStatus.New:D} OR "{cStatus}" = {InboxMessageStatus.Failed:D})
                      AND ("{cLockId}" IS NULL OR "{cLockExpirationTimeUtc}" IS NULL OR "{cLockExpirationTimeUtc}" < CURRENT_TIMESTAMP)
                    ORDER BY "{cScheduledStartIndexing}"
                    LIMIT {Options.MessagesToProcess}
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE {NameConst.SchemaName}.{NameConst.TableName} AS ie
                SET
                    "{cLockId}" = '{lockId:D}',
                    "{cLockExpirationTimeUtc}" = CURRENT_TIMESTAMP + ie."{cLockTimeout}"
                FROM cte
                WHERE ie."Id" = cte."Id"
                RETURNING ie.*;
                """;
    }
}
