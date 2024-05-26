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

namespace Dex.Cap.Outbox.Ef
{
    internal sealed class OutboxDataProviderEf<TDbContext>(
        TDbContext dbContext,
        IOptions<OutboxOptions> outboxOptions,
        ILogger<OutboxDataProviderEf<TDbContext>> logger,
        IOutboxRetryStrategy retryStrategy) : BaseOutboxDataProvider<TDbContext>(retryStrategy)
        where TDbContext : DbContext
    {
        private readonly OutboxOptions _outboxOptions = outboxOptions.Value;

        public override async Task ExecuteActionInTransaction<TState>(Guid correlationId, IOutboxService<TDbContext> outboxService, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(outboxService);
            ArgumentNullException.ThrowIfNull(action);

            await dbContext.ExecuteInTransactionScopeAsync(
                    (_dbContext: dbContext, outboxService, state),
                    async (st, ct) =>
                    {
                        var (context, outbox, outerState) = st;

                        if (context.ChangeTracker.HasChanges())
                            throw new UnsavedChangesDetectedException(context, "Can't start outbox action, unsaved changes detected");

                        try
                        {
                            var outboxContext = new OutboxContext<TDbContext, TState>(correlationId, outbox, context, outerState);
                            await action(ct, outboxContext).ConfigureAwait(false);

                            // проверяем есть ли в изменениях хоть одно аутбокс сообщение, если нет добавляем пустышку
                            var isOutboxMessageExists = context.ChangeTracker.Entries<OutboxEnvelope>()
                                .Any(x => x.State is EntityState.Added or EntityState.Modified);

                            if (!isOutboxMessageExists)
                            {
                                await outbox.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty, cancellationToken: ct).ConfigureAwait(false);
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

            var entityEntry = dbContext.Set<OutboxEnvelope>().Add(outboxEnvelope);
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
            return dbContext.ExecuteInTransactionScopeAsync(new { LockId = lockId }, async (state, token) =>
                {
                    var fetched = await dbContext.Set<OutboxEnvelope>()
                        .FromSqlRaw(GenerateSpecificSql())
                        .ToArrayAsync(token)
                        .ConfigureAwait(false);

                    if (fetched.Length == 0) return fetched;

                    var dbTime = await dbContext.Set<OutboxEnvelope>()
                        .Select(_ => new { DateTime.UtcNow })
                        .FirstAsync(token)
                        .ConfigureAwait(false);

                    foreach (var envelope in fetched)
                    {
                        envelope.LockId = state.LockId;
                        envelope.LockExpirationTimeUtc = DateTime.SpecifyKind(dbTime.UtcNow + envelope.LockTimeout, DateTimeKind.Utc);
                    }

                    await dbContext.SaveChangesAsync(token)
                        .ConfigureAwait(false);

                    foreach (var envelope in fetched)
                    {
                        dbContext.Entry(envelope).State = EntityState.Detached;
                    }

                    return fetched;
                },
                (state, token) => dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.CorrelationId == state.LockId, token),
                TransactionScopeOption.RequiresNew,
                IsolationLevel.ReadCommitted,
                20,
                cancellationToken: cancellationToken);
        }

        public override async Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
        {
            return await dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.CorrelationId == correlationId, cancellationToken).ConfigureAwait(false);
        }

        public override int GetFreeMessagesCount()
        {
            return dbContext.Set<OutboxEnvelope>()
                .Count(o => o.Retries < _outboxOptions.Retries && o.Status != OutboxMessageStatus.Succeeded);
        }


        // private

        /// <exception cref="RetryLimitExceededException"/>
        protected override async Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken)
        {
            await dbContext.ExecuteInTransactionScopeAsync(
                    (_dbContext: dbContext, lockedJob, _logger: logger),
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
                                // очищаем все что было в конетексте
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
                x => x.Id == messageId && x.LockId == lockId && (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
        }

        // TODO вынести в провайдер
        private string GenerateSpecificSql()
        {
            var providerName = dbContext.Database.ProviderName;
            if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                const string cScheduledStartIndexing = nameof(OutboxEnvelope.ScheduledStartIndexing);
                const string cRetries = nameof(OutboxEnvelope.Retries);
                const string cStatus = nameof(OutboxEnvelope.Status);
                const string cLockId = nameof(OutboxEnvelope.LockId);
                const string cLockExpirationTimeUtc = nameof(OutboxEnvelope.LockExpirationTimeUtc);
                const string cStartAtUtc = nameof(OutboxEnvelope.StartAtUtc);

                var sql = $@"
                        SELECT * 
                        FROM {NameConst.SchemaName}.{NameConst.TableName}
                        WHERE ""{cScheduledStartIndexing}"" IS NOT NULL
                          AND ""{cRetries}"" < {_outboxOptions.Retries}
                          AND (""{cStatus}"" = {OutboxMessageStatus.New:D} OR ""{cStatus}"" = {OutboxMessageStatus.Failed:D})
                          AND (""{cLockId}"" IS NULL OR ""{cLockExpirationTimeUtc}"" IS NULL OR ""{cLockExpirationTimeUtc}"" < CURRENT_TIMESTAMP)
                          AND CURRENT_TIMESTAMP >= ""{cStartAtUtc}""
                        ORDER BY ""{cScheduledStartIndexing}""
                        LIMIT {_outboxOptions.MessagesToProcess}
                        FOR UPDATE SKIP LOCKED;";
                return sql;
            }

            throw new NotSupportedException($"Provider {providerName}, not supported by GenerateSpecificSql()");
        }
    }
}