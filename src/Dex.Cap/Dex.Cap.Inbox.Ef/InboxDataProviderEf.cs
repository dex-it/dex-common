using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
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

        var affected = await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [
                    inboxEnvelope.Id,
                    inboxEnvelope.MessageId,
                    inboxEnvelope.ConsumerId,
                    inboxEnvelope.MessageType,
                    inboxEnvelope.Content,
                    (object?)inboxEnvelope.ActivityId ?? DBNull.Value,
                    inboxEnvelope.Retries,
                    (int)inboxEnvelope.Status,
                    inboxEnvelope.CreatedUtc,
                    // Оба поля заполняет конструктор InboxEnvelope, у принимаемого сообщения они всегда есть.
                    inboxEnvelope.StartAtUtc!.Value,
                    inboxEnvelope.ScheduledStartIndexing!.Value,
                    inboxEnvelope.LockTimeout
                ],
                cancellationToken)
            .ConfigureAwait(false);

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

    protected override Task CompleteJobAsync(IInboxLockedJob lockedJob, CancellationToken cancellationToken)
    {
        // ReadCommitted, а не RepeatableRead: корректность обеспечивает предикат владения арендой
        // прямо в UPDATE, а не уровень изоляции. Более строгий уровень здесь запрещал бы
        // переиспользование этого метода внутри транзакции обработчика (Common.Ef не позволяет
        // повышать уровень у вложенной транзакции), а именно там фиксируется успех.
        return dbContext.ExecuteInTransactionAsync(
            (DbContext: dbContext, LockedJob: lockedJob, Logger: logger),
            static async (state, ct) =>
            {
                var (context, job, log) = state;
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

                if (affected == 0)
                {
                    // Аренда истекла и сообщение уже перехвачено другим обработчиком. Молча это глотать нельзя:
                    // значит LockTimeout меньше реального времени обработки, и сообщения обрабатываются дважды.
                    log.LogWarning(
                        "Inbox job {JobId} can not be completed: the lock has expired and the message was taken by another handler. " +
                        "Increase LockTimeout above the real processing time",
                        envelope.Id);
                }
            },
            static async (state, ct) =>
            {
                var (context, job, _) = state;
                var envelope = job.Envelope;

                var existLocked = await context.Set<InboxEnvelope>()
                    .AnyAsync(x => x.Id == envelope.Id && x.LockId == job.LockId, ct)
                    .ConfigureAwait(false);

                return !existLocked;
            },
            EfTransactionOptions.Default,
            cancellationToken: cancellationToken);
    }

    private void EnsureNpgsql()
    {
        var providerName = dbContext.Database.ProviderName;

        if (providerName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
            throw new NotSupportedException($"The provider {providerName} is not supported.");
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
