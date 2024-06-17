using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService<TDbContext>(
        IOutboxDataProvider<TDbContext> outboxDataProvider,
        IOutboxSerializer serializer,
        IOutboxTypeDiscriminator discriminator) : IOutboxService<TDbContext>
    {
        private readonly IOutboxDataProvider<TDbContext>
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));

        private readonly IOutboxSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        private readonly IOutboxTypeDiscriminator _discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));

        public Task ExecuteOperationAsync<TState>(Guid correlationId, TState state, Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            return _outboxDataProvider.ExecuteActionInTransaction(correlationId, this, state, action, cancellationToken);
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, DateTime? startAtUtc, TimeSpan? lockTimeout, CancellationToken cancellationToken)
            where T : IOutboxMessage
        {
            var messageType = message.GetType();
            if (messageType != typeof(EmptyOutboxMessage) && message.MessageId == default)
                throw new InvalidOperationException("MessageId can't be empty");

            var envelopeId = message.MessageId;
            var msgBody = _serializer.Serialize(messageType, message);
            var discriminator = _discriminator.ResolveDiscriminator(messageType);
            var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId, discriminator, msgBody, startAtUtc, lockTimeout);
            await _outboxDataProvider.Add(outboxEnvelope, cancellationToken).ConfigureAwait(false);

            return message.MessageId;
        }

        public async Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return await _outboxDataProvider.IsExists(correlationId, cancellationToken).ConfigureAwait(false);
        }
    }
}