using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Exceptions;
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
    IOutboxTypeDiscriminator outboxTypeDiscriminator)
    : BaseOutboxDataProvider<IEfTransactionOptions, TDbContext>(retryStrategy)
    where TDbContext : DbContext
{
    private OutboxOptions Options => outboxOptions.Value;

    public override Task ExecuteActionInTransaction<TState>(
        Guid correlationId,
        IOutboxService<IEfTransactionOptions, TDbContext> outboxService,
        TState state,
        Func<IOutboxContext<TDbContext, TState>, CancellationToken, Task> action,
        IEfTransactionOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outboxService);
        ArgumentNullException.ThrowIfNull(action);

        options ??= new EfTransactionOptions();

        return dbContext.ExecuteInTransactionScopeAsync(
            state: (_dbContext: dbContext, outboxService, state),
            operation: async (st, ct) =>
            {
                var (context, outbox, outerState) = st;

                if (context.ChangeTracker.HasChanges())
                    throw new UnsavedChangesDetectedException(context,
                        "Can't start outbox action, unsaved changes detected");

                var outboxContext =
                    new OutboxContext<IEfTransactionOptions, TDbContext, TState>(correlationId, outbox, context,
                        outerState);
                await action(outboxContext, ct).ConfigureAwait(false);

                // проверяем есть ли в изменениях хоть одно аутбокс сообщение, если нет добавляем пустышку
                var isOutboxMessageExists = context.ChangeTracker.Entries<OutboxEnvelope>()
                    .Any(x => x.State is EntityState.Added or EntityState.Modified);

                if (!isOutboxMessageExists)
                {
                    await outbox.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty, cancellationToken: ct)
                        .ConfigureAwait(false);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            },
            verifySucceeded: async (_, ct) => await IsExists(correlationId, ct).ConfigureAwait(false),
            transactionScopeOption: options.TransactionScopeOption,
            isolationLevel: options.IsolationLevel,
            timeoutInSeconds: options.TimeoutInSeconds,
            clearChangeTrackerOnRetry: options.ClearChangeTrackerOnRetry,
            cancellationToken: cancellationToken);
    }

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

        return outboxEnvelopes.Select(x => (IOutboxLockedJob)new OutboxLockedJob(x)).ToArray();
    }

    public override Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
    {
        var lockId = Guid.NewGuid(); // Ключ идемпотентности.
        return dbContext.ExecuteInTransactionScopeAsync(
            new { LockId = lockId, DbContext = dbContext },
            async (state, token) =>
            {
                var sql = GenerateFetchPlatformSpecificSql(state.LockId);

                var lockedEnvelopes = await state.DbContext
                    .Set<OutboxEnvelope>()
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken: token)
                    .ConfigureAwait(false);

                return lockedEnvelopes;
            },
            async (state, token) => await state.DbContext.Set<OutboxEnvelope>()
                .AnyAsync(x => x.CorrelationId == state.LockId, token).ConfigureAwait(false),
            TransactionScopeOption.RequiresNew,
            IsolationLevel.ReadCommitted,
            (uint)Options.GetFreeMessagesTimeout.TotalSeconds,
            clearChangeTrackerOnRetry: false,
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
            isolationLevel: IsolationLevel.RepeatableRead,
            clearChangeTrackerOnRetry: false,
            cancellationToken: cancellationToken);

        static Expression<Func<OutboxEnvelope, bool>> WhereLockId(Guid messageId, Guid lockId) =>
            x => x.Id == messageId && x.LockId == lockId &&
                 (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
    }

    // TODO вынести в провайдер
    private string GenerateFetchPlatformSpecificSql(Guid lockId)
    {
        var providerName = dbContext.Database.ProviderName;
        if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            const string cScheduledStartIndexing = nameof(OutboxEnvelope.ScheduledStartIndexing);
            const string cRetries = nameof(OutboxEnvelope.Retries);
            const string cStatus = nameof(OutboxEnvelope.Status);
            const string cLockId = nameof(OutboxEnvelope.LockId);
            const string cLockExpirationTimeUtc = nameof(OutboxEnvelope.LockExpirationTimeUtc);
            const string cLockTimeout = nameof(OutboxEnvelope.LockTimeout);
            const string cStartAtUtc = nameof(OutboxEnvelope.StartAtUtc);
            const string cMessageType = nameof(OutboxEnvelope.MessageType);

            var discriminators = outboxTypeDiscriminator.GetDiscriminators();
            var discriminatorsSql = string.Join(", ", discriminators.Select(d => $"'{d}'"));

            var sql = $@"
                WITH cte AS (
                    SELECT ""Id""
                    FROM {NameConst.SchemaName}.{NameConst.TableName}
                    WHERE ""{cScheduledStartIndexing}"" IS NOT NULL
                      AND ""{cMessageType}"" IN ({discriminatorsSql})
                      AND CURRENT_TIMESTAMP >= ""{cStartAtUtc}""
                      AND ""{cRetries}"" < {Options.Retries}
                      AND (""{cStatus}"" = {OutboxMessageStatus.New:D} OR ""{cStatus}"" = {OutboxMessageStatus.Failed:D})
                      AND (""{cLockId}"" IS NULL OR ""{cLockExpirationTimeUtc}"" IS NULL OR ""{cLockExpirationTimeUtc}"" < CURRENT_TIMESTAMP)
                    ORDER BY ""{cScheduledStartIndexing}""
                    LIMIT {Options.MessagesToProcess}
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE {NameConst.SchemaName}.{NameConst.TableName} AS oe
                SET 
                    ""{cLockId}"" = '{lockId:D}',
                    ""{cLockExpirationTimeUtc}"" = CURRENT_TIMESTAMP + oe.""{cLockTimeout}""
                FROM cte
                WHERE oe.""Id"" = cte.""Id""
                RETURNING oe.*;
            ";
            return sql;
        }

        throw new NotSupportedException($"The provider {providerName} is not supported.");
    }
}