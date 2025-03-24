﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService<TDbContext> : IOutboxService<TDbContext>
    {
        public IOutboxTypeDiscriminator Discriminator { get; }

        private readonly IOutboxDataProvider<TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(
            IOutboxDataProvider<TDbContext> outboxDataProvider,
            IOutboxSerializer serializer,
            IOutboxTypeDiscriminator discriminator)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
        }

        public Task ExecuteOperationAsync<TState>(Guid correlationId, TState state, Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            return _outboxDataProvider.ExecuteActionInTransaction(correlationId, this, state, action, cancellationToken);
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, DateTime? startAtUtc = null, TimeSpan? lockTimeout = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var messageType = message.GetType();
            var envelopeId = messageType == typeof(EmptyOutboxMessage)
                ? Guid.Empty
                : Guid.NewGuid();

            var msgBody = _serializer.Serialize(messageType, message);
            var discriminator = Discriminator.ResolveDiscriminator(messageType);
            var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId, discriminator, msgBody, startAtUtc, lockTimeout);
            await _outboxDataProvider.Add(outboxEnvelope, cancellationToken).ConfigureAwait(false);

            return envelopeId;
        }

        public async Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            return await _outboxDataProvider.IsExists(correlationId, cancellationToken).ConfigureAwait(false);
        }
    }
}