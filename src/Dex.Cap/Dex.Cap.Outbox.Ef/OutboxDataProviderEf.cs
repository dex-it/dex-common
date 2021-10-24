﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Helpers;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Ef
{
    internal class OutboxDataProviderEf<TDbContext> : BaseOutboxDataProvider where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderEf(TDbContext dbContext, IOptions<OutboxOptions> outboxOptions, ILogger<OutboxDataProviderEf<TDbContext>> logger)
        {
            if (outboxOptions is null)
            {
                throw new ArgumentNullException(nameof(outboxOptions));
            }

            _dbContext = dbContext;
            _logger = logger;
            _outboxOptions = outboxOptions.Value;
        }

        public override async Task ExecuteUsefulAndSaveOutboxActionIntoTransaction<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteInTransactionAsync(
                async () =>
                {
                    _dbContext.ChangeTracker.Clear();
                    var context = await usefulAction(cancellationToken).ConfigureAwait(false);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await createOutboxData(cancellationToken, context).ConfigureAwait(false);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                },
                () => IsExists(correlationId, cancellationToken)).ConfigureAwait(false);
        }

        public override Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            if (outboxEnvelope == null)
            {
                throw new ArgumentNullException(nameof(outboxEnvelope));
            }

            var entityEntry = _dbContext.Set<OutboxEnvelope>().Add(outboxEnvelope);
            return Task.FromResult(entityEntry.Entity);
        }

        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="RetryLimitExceededException"/>
        public override async IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var freeMessages = await GetFreeMessages(_outboxOptions.MessagesToProcess, cancellationToken).ConfigureAwait(false);

            // Будем пытаться захватить блокировку по одному сообщению.
            foreach (var freeMessage in freeMessages)
            {
                var lockedJob = await TryCreateJob(freeMessage, cancellationToken).ConfigureAwait(false);

                if (lockedJob != null)
                {
                    if (!lockedJob.LockToken.IsCancellationRequested)
                    {
                        yield return lockedJob;
                    }
                    else
                    {
                        lockedJob.Dispose();
                    }
                }
            }
        }

        public override async Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
        {
            return await _dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == correlationId, cancellationToken).ConfigureAwait(false);
        }


        /// <exception cref="RetryLimitExceededException"/>
        protected override async Task CompleteJobAsync(IOutboxLockedJob lockedJob, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransactionAsync((_dbContext, lockedJob, _logger), static async (state, ct) =>
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
                        }
                        else
                        {
                            // Истекло время блокировки.
                        }
                    },
                    static async (state, ct) =>
                    {
                        var (dbContext, outboxJob, _) = state;

                        bool existLocked = await dbContext.Set<OutboxEnvelope>()
                            .AnyAsync(WhereLockId(outboxJob.Envelope.Id, outboxJob.LockId), ct)
                            .ConfigureAwait(false);

                        return !existLocked;
                    },
                    IsolationLevel.RepeatableRead,
                    cancellationToken)
                .ConfigureAwait(false);

            static Expression<Func<OutboxEnvelope, bool>> WhereLockId(Guid messageId, Guid lockId) =>
                x => x.Id == messageId && x.LockId == lockId && (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
        }

        /// <summary>
        /// Пытается захватить блокировку над сообщением Outbox и создать задачу с превентивным таймаутом.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        private async Task<OutboxLockedJob?> TryCreateJob(OutboxEnvelope freeMessage, CancellationToken cancellationToken)
        {
            var lockId = Guid.NewGuid(); // Ключ идемпотентности.
            CancellationTokenSource? cts = null;

            if (freeMessage.LockTimeout != Timeout.InfiniteTimeSpan)
            {
                var timeout = freeMessage.LockTimeout - TimeSpan.FromSeconds(10);
                Debug.Assert(timeout > TimeSpan.Zero, "Таймаут должен быть больше 10 секунд.");

                // Будем отсчитывать время жизни блокировки ещё перед запросом.
                cts = new CancellationTokenSource(timeout);
            }

            try
            {
                var lockedMessage = await TryLockMessage(freeMessage.Id, lockId, cts?.Token ?? default, cancellationToken).ConfigureAwait(false);

                return lockedMessage != null
                    ? new OutboxLockedJob(lockedMessage, lockId, NullableHelper.SetNull(ref cts))
                    : null;
            }
            finally
            {
                cts?.Dispose();
            }
        }

        /// <exception cref="OperationCanceledException"/>
        private async Task<OutboxEnvelope?> TryLockMessage(Guid freeMessageId, Guid lockId, CancellationToken jobTimeout, CancellationToken cancellationToken)
        {
            if (jobTimeout.CanBeCanceled)
            {
                try
                {
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(jobTimeout, cancellationToken))
                    {
                        return await TryLockMessageCore(freeMessageId, lockId, cts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (jobTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    return null; // Не успели заблокировать задачу за превентивное время таймаута.
                }
            }
            else
            {
                return await TryLockMessageCore(freeMessageId, lockId, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Выполняет стратегию оптимистичного захвата эксклюзивного доступа к задаче Outbox.
        /// </summary>
        /// <exception cref="RetryLimitExceededException"/>
        private async Task<OutboxEnvelope?> TryLockMessageCore(Guid freeMessageId, Guid lockId, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            var message = await strategy.ExecuteInTransactionAsync((_dbContext, freeMessageId, lockId, _logger),
                    static async (state, ct) =>
                    {
                        var (dbContext, freeMessageId, lockId, logger) = state;

                        var lockedJob = await dbContext.Set<OutboxEnvelope>()
                            .Where(WhereFree(freeMessageId))
                            .Select(x => new
                            {
                                DbNow = DateTime.Now, // Вытащить текущее время БД что-бы синхронизироваться.
                                JobDb = x
                            })
                            .FirstOrDefaultAsync(ct)
                            .ConfigureAwait(false);

                        if (lockedJob != null)
                        {
                            Debug.Assert(lockedJob.DbNow.Kind != DateTimeKind.Unspecified, "Опасно работать с неопределённой датой");

                            logger.LogTrace("Attempt to lock the message {MessageId}", freeMessageId);

                            lockedJob.JobDb.LockId = lockId;
                            lockedJob.JobDb.LockExpirationTimeUtc = (lockedJob.DbNow + lockedJob.JobDb.LockTimeout).ToUniversalTime();

                            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                            logger.LogTrace("Message is successfully captured {MessageId}", freeMessageId);

                            dbContext.Entry(lockedJob.JobDb).State = EntityState.Detached;

                            return lockedJob.JobDb;
                        }
                        else
                        {
                            logger.LogTrace("Another thread overtook and captured this message. {MessageId}", freeMessageId);
                            return null;
                        }
                    },
                    static async (state, ct) =>
                    {
                        var (dbContext, freeMessageId, lockId, logger) = state;

                        var succeded = await dbContext.Set<OutboxEnvelope>()
                            .AnyAsync(x => x.Id == freeMessageId && x.LockId == lockId, ct)
                            .ConfigureAwait(false);

                        return succeded;
                    },
                    IsolationLevel.RepeatableRead,
                    cancellationToken)
                .ConfigureAwait(false);

            return message;

            static Expression<Func<OutboxEnvelope, bool>> WhereFree(Guid messageId)
            {
                return (OutboxEnvelope x) =>
                    x.Id == messageId && (x.LockId == null || x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc < DateTime.UtcNow);
            }
        }

        /// <exception cref="OperationCanceledException"/>
        private async Task<OutboxEnvelope[]> GetFreeMessages(int limit, CancellationToken cancellationToken)
        {
            var potentialFree = await _dbContext.Set<OutboxEnvelope>()
                .Where(o => o.Retries < _outboxOptions.Retries && (o.Status == OutboxMessageStatus.New || o.Status == OutboxMessageStatus.Failed))
                .Where(x => x.LockId == null || x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc < DateTime.UtcNow)
                .OrderBy(o => o.CreatedUtc)
                .Take(limit)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            return potentialFree;
        }
    }
}