﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxService
    {
        /// <summary>
        /// Outbox Type Discriminator
        /// </summary>
        IOutboxTypeDiscriminator Discriminator { get; }

        /// <summary>
        /// Perform only publish outbox message to queue.
        /// This method don't check Transaction, only append outbox message to change context.
        /// NOTE. LockTimeout must be greater time of process message, against it lead to cycle process.
        /// Default value is 30sec. Minimum value 10 sec.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, DateTime? startAtUtc = null, TimeSpan? lockTimeout = null,
            CancellationToken cancellationToken = default)
            where T : IOutboxMessage;

        /// <summary>
        /// Check if operation with correlationId already exists.
        /// </summary>
        Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken = default);
    }

    public interface IOutboxService<out TDbContext> : IOutboxService
    {
        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="action">A delegate representing an executable operation that returns Task />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>CorrelationId</returns>
        Task ExecuteOperationAsync(Guid correlationId, Func<CancellationToken, IOutboxContext<TDbContext, object?>, Task> action,
            CancellationToken cancellationToken = default)
        {
            return ExecuteOperationAsync(correlationId, default, action, cancellationToken);
        }

        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="state">The state that will be passed to the usefulAction.</param>
        /// <param name="action">A delegate representing an executable operation that returns Task />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <returns>CorrelationId</returns>
        Task ExecuteOperationAsync<TState>(Guid correlationId, TState state, Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action,
            CancellationToken cancellationToken = default);
    }
}