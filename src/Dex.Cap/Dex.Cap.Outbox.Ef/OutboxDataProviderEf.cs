using System;
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
    public class OutboxDataProviderEf<TDbContext> : BaseOutboxDataProvider where TDbContext : DbContext
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

        public override async Task ExecuteInTransactionAsync(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteInTransactionAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await operation(cancellationToken).ConfigureAwait(false);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }, () => IsExistsAsync(correlationId, cancellationToken)).ConfigureAwait(false);
        }

        public override Task<OutboxEnvelope> AddAsync(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));

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

        public override Task<bool> IsExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return _dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == correlationId, cancellationToken);
        }

        /// <exception cref="RetryLimitExceededException"/>
        protected override async Task CompleteJobAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransactionAsync((_dbContext, outboxJob), static async (state, ct) =>
            {
                var (dbContext, outboxJob) = state;

                var job = await dbContext.Set<OutboxEnvelope>()
                    .Where(WhereLockId(outboxJob.Envelope.Id, outboxJob.LockId))
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (job != null)
                {
                    job.Status = outboxJob.Envelope.Status;
                    job.Updated = outboxJob.Envelope.Updated;
                    job.Retries = outboxJob.Envelope.Retries;
                    job.ErrorMessage = outboxJob.Envelope.ErrorMessage;
                    job.Error = outboxJob.Envelope.Error;
                    job.LockId = null;

                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    // Истекло время блокировки.
                }
            },
            static async (state, ct) =>
            {
                var (dbContext, outboxJob) = state;

                bool hasLocked = await dbContext.Set<OutboxEnvelope>()
                    .AnyAsync(WhereLockId(outboxJob.Envelope.Id, outboxJob.LockId), ct)
                    .ConfigureAwait(false);

                return !hasLocked;
            },
            IsolationLevel.RepeatableRead,
            cancellationToken)
                .ConfigureAwait(false);

            static Expression<Func<OutboxEnvelope, bool>> WhereLockId(Guid messageId, Guid lockId)
            {
                return (OutboxEnvelope x) => x.Id == messageId && x.LockId == lockId && (x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc > DateTime.UtcNow);
            }
        }

        public override async Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            int removedCount = 0;

            Repeat:
            var finishedMessages = await GetFinishedMessages(limit: 1000, olderThan: olderThan, cancellationToken).ConfigureAwait(false);

            if (finishedMessages.Length == 0)
            {
                return removedCount;
            }

            foreach (var message in finishedMessages)
            {
                await DeleteMessage(message, cancellationToken).ConfigureAwait(false);
            }

            removedCount += finishedMessages.Length;
            goto Repeat;
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

            return await strategy.ExecuteInTransactionAsync((_dbContext, freeMessageId, lockId), static async (state, ct) =>
            {
                var (dbContext, freeMessageId, lockId) = state;

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

                    //_logger.LogTrace("Попытка захватить задачу {MessageId}", potentialFreeJobId);

                    lockedJob.JobDb.LockId = lockId;
                    lockedJob.JobDb.LockExpirationTimeUtc = (lockedJob.DbNow + lockedJob.JobDb.LockTimeout).ToUniversalTime();

                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

                    var tstate = dbContext.Entry(lockedJob.JobDb).State;
                    dbContext.Entry(lockedJob.JobDb).State = EntityState.Detached;

                    //_logger.LogTrace("Задача захвачена {MessageId}", potentialFreeJobId);
                    // Thread.Sleep(2000);

                    return lockedJob.JobDb;
                }
                else
                {
                    // Другой поток обогнал и захватил эту задачу.
                    return null;
                }
            },
            static async (state, ct) =>
            {
                var (dbContext, freeMessageId, lockId) = state;
                bool succeded = await dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == freeMessageId && x.LockId == lockId, ct).ConfigureAwait(false);
                return succeded;
            },
            IsolationLevel.RepeatableRead,
            cancellationToken)
                .ConfigureAwait(false);

            static Expression<Func<OutboxEnvelope, bool>> WhereFree(Guid messageId)
            {
                return (OutboxEnvelope x) => x.Id == messageId && (x.LockId == null || x.LockExpirationTimeUtc == null || x.LockExpirationTimeUtc < DateTime.UtcNow);
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

        private async Task<Guid[]> GetFinishedMessages(int limit, TimeSpan olderThan, CancellationToken cancellationToken)
        {
            var finishedMessages = await _dbContext.Set<OutboxEnvelope>()
                .Where(o => o.Status == OutboxMessageStatus.Succeeded && o.CreatedUtc + olderThan < DateTime.UtcNow)
                .OrderBy(o => o.CreatedUtc)
                .Take(limit)
                .AsNoTracking()
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            return finishedMessages;
        }

        private async Task DeleteMessage(Guid messageId, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransactionAsync((_dbContext, messageId), static async (state, ct) =>
            {
                var (dbContext, messageId) = state;

                var lockedJob = await dbContext.Set<OutboxEnvelope>()
                    .Where(x => x.Id == messageId)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (lockedJob != null)
                {
                    dbContext.Set<OutboxEnvelope>().Remove(lockedJob);
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                }
            },
            static async (state, ct) =>
            {
                var (dbContext, messageId) = state;
                bool exists = await dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == messageId, ct).ConfigureAwait(false);
                return !exists;
            },
            IsolationLevel.RepeatableRead,
            cancellationToken)
                .ConfigureAwait(false);
        }
    }
}