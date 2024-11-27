using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
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

internal sealed class OutboxDataProviderEf<TDbContext> : BaseOutboxDataProvider<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<OutboxDataProviderEf<TDbContext>> _logger;
    private readonly OutboxOptions _outboxOptions;
    private readonly IOutboxTypeDiscriminator _outboxTypeDiscriminator;

    public OutboxDataProviderEf(
        TDbContext dbContext,
        IOptions<OutboxOptions> outboxOptions,
        ILogger<OutboxDataProviderEf<TDbContext>> logger,
        IOutboxRetryStrategy retryStrategy,
        IOutboxTypeDiscriminator outboxTypeDiscriminator) : base(retryStrategy)
    {
        _dbContext = dbContext;
        _outboxOptions = outboxOptions.Value;
        _logger = logger;
        _outboxTypeDiscriminator = outboxTypeDiscriminator;
    }

    public override async Task ExecuteActionInTransaction<TState>(Guid correlationId,
        IOutboxService<TDbContext> outboxService, TState state,
        Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outboxService);
        ArgumentNullException.ThrowIfNull(action);

        await _dbContext.ExecuteInTransactionScopeAsync(
                (_dbContext, outboxService, state),
                async (st, ct) =>
                {
                    var (context, outbox, outerState) = st;

                    if (context.ChangeTracker.HasChanges())
                        throw new UnsavedChangesDetectedException(context,
                            "Can't start outbox action, unsaved changes detected");

                    try
                    {
                        var outboxContext =
                            new OutboxContext<TDbContext, TState>(correlationId, outbox, context, outerState);
                        await action(ct, outboxContext).ConfigureAwait(false);

                        // проверяем есть ли в изменениях хоть одно аутбокс сообщение, если нет добавляем пустышку
                        var isOutboxMessageExists = context.ChangeTracker.Entries<OutboxEnvelope>()
                            .Any(x => x.State is EntityState.Added or EntityState.Modified);

                        if (!isOutboxMessageExists)
                        {
                            await outbox.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty, cancellationToken: ct)
                                .ConfigureAwait(false);
                        }

                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        context.ChangeTracker.Clear();
                    }
                },
                async (_, ct) => await IsExists(correlationId, ct).ConfigureAwait(false),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public override Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outboxEnvelope);
        cancellationToken.ThrowIfCancellationRequested();

        var entityEntry = _dbContext.Set<OutboxEnvelope>().Add(outboxEnvelope);
        return Task.FromResult(entityEntry.Entity);
    }

    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="RetryLimitExceededException"/>
    public override async Task<IOutboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken)
    {
        var outboxEnvelopes = await GetFreeMessages(cancellationToken)
            .ConfigureAwait(false);

        return outboxEnvelopes.Select(x => (IOutboxLockedJob)new OutboxLockedJob(x)).ToArray();
    }

    
    public override Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
    {
        var lockId = Guid.NewGuid(); // Ключ идемпотентности.
        return _dbContext.ExecuteInTransactionScopeAsync(new { LockId = lockId }, async (state, token) =>
            {
                var sql = GenerateFetchPlatformSpecificSql(state.LockId);

                var lockedEnvelopes = await _dbContext.Set<OutboxEnvelope>()
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: token)
                    .ConfigureAwait(false);

                return lockedEnvelopes.ToArray();
            },
            (state, token) => _dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.CorrelationId == state.LockId, token),
            TransactionScopeOption.RequiresNew,
            IsolationLevel.ReadCommitted,
            (uint)_outboxOptions.GetFreeMessagesTimeout.TotalSeconds,
            cancellationToken: cancellationToken);
    }

    public override async Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.CorrelationId == correlationId, cancellationToken)
            .ConfigureAwait(false);
    }

    public override int GetFreeMessagesCount()
    {
        return _dbContext.Set<OutboxEnvelope>()
            .Count(o => o.Retries < _outboxOptions.Retries && o.Status != OutboxMessageStatus.Succeeded);
    }


    // private

    /// <exception cref="RetryLimitExceededException"/>
    protected override async Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken)
    {
        await _dbContext.ExecuteInTransactionScopeAsync(
                (_dbContext, lockedJob, _logger),
                static async (state, ct) =>
                {
                    var (dbContext, lockedJob, logger) = state;

                    var job = await dbContext.Set<OutboxEnvelope>()
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
                            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                        }
                        catch (DbUpdateException e)
                        {
                            logger.LogWarning(e, "Job {JobId} can not complete outbox action", job.Id);
                            // очищаем все что было в контексте
                            dbContext.ChangeTracker.Clear();
                            dbContext.Update(job);
                            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                        }
                        finally
                        {
                            dbContext.ChangeTracker.Clear();
                        }
                    }

                    // Истекло время блокировки.
                },
                static async (state, ct) =>
                {
                    var (dbContext, outboxJob, _) = state;

                    var existLocked = await dbContext.Set<OutboxEnvelope>()
                        .AnyAsync(WhereLockId(outboxJob.Envelope.Id, outboxJob.LockId), ct)
                        .ConfigureAwait(false);

                    return !existLocked;
                },
                isolationLevel: IsolationLevel.RepeatableRead,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        static Expression<Func<OutboxEnvelope, bool>> WhereLockId(Guid messageId, Guid lockId) =>
            x => x.Id == messageId && x.LockId == lockId &&
                 (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
    }

    // TODO вынести в провайдер
    private string GenerateFetchPlatformSpecificSql(Guid lockId)
    {
        var providerName = _dbContext.Database.ProviderName;
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
            
            var discriminators = _outboxTypeDiscriminator.Discriminators.Keys.ToArray();
            var discriminatorsSql = string.Join(", ", discriminators.Select(d => $"'{d}'"));


            var sql = $@"
                WITH cte AS (
                    SELECT ""Id""
                    FROM {NameConst.SchemaName}.{NameConst.TableName}
                    WHERE ""{cScheduledStartIndexing}"" IS NOT NULL
                      AND ""{cMessageType}"" IN ({discriminatorsSql})
                      AND CURRENT_TIMESTAMP >= ""{cStartAtUtc}""
                      AND ""{cRetries}"" < {_outboxOptions.Retries}
                      AND (""{cStatus}"" = {OutboxMessageStatus.New:D} OR ""{cStatus}"" = {OutboxMessageStatus.Failed:D})
                      AND (""{cLockId}"" IS NULL OR ""{cLockExpirationTimeUtc}"" IS NULL OR ""{cLockExpirationTimeUtc}"" < CURRENT_TIMESTAMP)
                    ORDER BY ""{cScheduledStartIndexing}""
                    LIMIT {_outboxOptions.MessagesToProcess}
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