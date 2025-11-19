using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Ef;

internal sealed class OutboxDataProviderEf<TDbContext>(
    TDbContext dbContext,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<OutboxDataProviderEf<TDbContext>> logger,
    IOutboxRetryStrategy retryStrategy,
    IOutboxTypeDiscriminatorProvider discriminatorProvider) : BaseOutboxDataProvider(retryStrategy) where TDbContext : DbContext
{
    private EfTransactionOptions? _transactionOptions;

    private OutboxOptions Options => outboxOptions.Value;

    public override Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outboxEnvelope);
        cancellationToken.ThrowIfCancellationRequested();

        var entityEntry = dbContext.Set<OutboxEnvelope>().Add(outboxEnvelope);
        return Task.FromResult(entityEntry.Entity);
    }

    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="RetryLimitExceededException"/>
    public override async Task<IOutboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken)
    {
        var outboxEnvelopes = await GetFreeMessages(cancellationToken).ConfigureAwait(false);

        return outboxEnvelopes.Select(IOutboxLockedJob (x) => new OutboxLockedJob(x)).ToArray();
    }

    public override Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
    {
        _transactionOptions ??= new EfTransactionOptions
        {
            TransactionScopeOption = TransactionScopeOption.RequiresNew,
            IsolationLevel = IsolationLevel.ReadCommitted,
            TimeoutInSeconds = (uint)Options.GetFreeMessagesTimeout.TotalSeconds
        };

        var lockId = Guid.NewGuid(); // Ключ идемпотентности.
        return dbContext.ExecuteInTransactionScopeAsync(
            new {LockId = lockId, DbContext = dbContext},
            async (state, token) =>
            {
                var sql = GenerateFetchPlatformSpecificSql(state.LockId);

                if (sql is null)
                    return [];

                var lockedEnvelopes = await state.DbContext
                    .Set<OutboxEnvelope>()
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken: token)
                    .ConfigureAwait(false);

                return lockedEnvelopes;
            },
            async (state, token) => await state.DbContext.Set<OutboxEnvelope>()
                .AnyAsync(x => x.LockId == state.LockId, token).ConfigureAwait(false),
            _transactionOptions,
            cancellationToken: cancellationToken);
    }

    public override Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
    {
        return dbContext
            .Set<OutboxEnvelope>()
            .AnyAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }

    public override int GetFreeMessagesCount()
    {
        return dbContext
            .Set<OutboxEnvelope>()
            .Count(o => o.Retries < Options.Retries && o.Status != OutboxMessageStatus.Succeeded);
    }

    /// <exception cref="RetryLimitExceededException"/>
    protected override Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken)
    {
        return dbContext.ExecuteInTransactionScopeAsync(
            (_dbContext: dbContext, lockedJob, _logger: logger),
            static async (state, ct) =>
            {
                var (context, lockedJob, logger) = state;

                var job = await context
                    .Set<OutboxEnvelope>()
                    .Where(WhereLockId(lockedJob.Envelope.Id, lockedJob.LockId))
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (job != null)
                {
                    job.Status = lockedJob.Envelope.Status;
                    job.Updated = lockedJob.Envelope.Updated;
                    job.Retries = lockedJob.Envelope.Retries;
                    job.ErrorMessage = lockedJob.Envelope.ErrorMessage;
                    job.Error = lockedJob.Envelope.Error;
                    job.StartAtUtc = lockedJob.Envelope.StartAtUtc;
                    job.ScheduledStartIndexing = lockedJob.Envelope.ScheduledStartIndexing;
                    job.LockId = null;

                    try
                    {
                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                    catch (DbUpdateException e)
                    {
                        logger.LogWarning(e, "Job {JobId} can not complete outbox action", job.Id);
                        // очищаем все что было в контексте
                        context.ChangeTracker.Clear();
                        context.Update(job);
                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        context.ChangeTracker.Clear();
                    }
                }

                // Истекло время блокировки.
            },
            static async (state, ct) =>
            {
                var (context, outboxJob, _) = state;

                var existLocked = await context.Set<OutboxEnvelope>()
                    .AnyAsync(WhereLockId(outboxJob.Envelope.Id, outboxJob.LockId), ct)
                    .ConfigureAwait(false);

                return !existLocked;
            },
            EfTransactionOptions.DefaultRepeatableRead,
            cancellationToken: cancellationToken);

        static Expression<Func<OutboxEnvelope, bool>> WhereLockId(Guid messageId, Guid lockId) =>
            x => x.Id == messageId && x.LockId == lockId &&
                 (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
    }

    // TODO вынести в провайдер
    private string? GenerateFetchPlatformSpecificSql(Guid lockId)
    {
        var providerName = dbContext.Database.ProviderName;

        if (providerName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
            throw new NotSupportedException($"The provider {providerName} is not supported.");

        const string cScheduledStartIndexing = nameof(OutboxEnvelope.ScheduledStartIndexing);
        const string cRetries = nameof(OutboxEnvelope.Retries);
        const string cStatus = nameof(OutboxEnvelope.Status);
        const string cLockId = nameof(OutboxEnvelope.LockId);
        const string cLockExpirationTimeUtc = nameof(OutboxEnvelope.LockExpirationTimeUtc);
        const string cLockTimeout = nameof(OutboxEnvelope.LockTimeout);
        const string cStartAtUtc = nameof(OutboxEnvelope.StartAtUtc);
        const string cMessageType = nameof(OutboxEnvelope.MessageType);

        var discriminators = discriminatorProvider.SupportedDiscriminators;

        if (discriminators.Count is 0)
            return null;

        var discriminatorsSql = string.Join(", ", discriminators.Select(d => $"'{d}'"));

        return $"""
                WITH cte AS (
                    SELECT "Id"
                    FROM {NameConst.SchemaName}.{NameConst.TableName}
                    WHERE "{cScheduledStartIndexing}" IS NOT NULL
                      AND "{cMessageType}" IN ({discriminatorsSql})
                      AND CURRENT_TIMESTAMP >= "{cStartAtUtc}"
                      AND "{cRetries}" < {Options.Retries}
                      AND ("{cStatus}" = {OutboxMessageStatus.New:D} OR "{cStatus}" = {OutboxMessageStatus.Failed:D})
                      AND ("{cLockId}" IS NULL OR "{cLockExpirationTimeUtc}" IS NULL OR "{cLockExpirationTimeUtc}" < CURRENT_TIMESTAMP)
                    ORDER BY "{cScheduledStartIndexing}"
                    LIMIT {Options.MessagesToProcess}
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE {NameConst.SchemaName}.{NameConst.TableName} AS oe
                SET 
                    "{cLockId}" = '{lockId:D}',
                    "{cLockExpirationTimeUtc}" = CURRENT_TIMESTAMP + oe."{cLockTimeout}"
                FROM cte
                WHERE oe."Id" = cte."Id"
                RETURNING oe.*;
                """;
    }
}