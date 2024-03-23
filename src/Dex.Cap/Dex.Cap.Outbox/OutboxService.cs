using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService<TDbContext> : IOutboxService<TDbContext>
    {
        private readonly IOutboxDataProvider<TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;
        private readonly IOutboxTypeDiscriminator _discriminator;

        public OutboxService(IOutboxDataProvider<TDbContext> outboxDataProvider, IOutboxSerializer serializer, IOutboxTypeDiscriminator discriminator)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
        }

        public async Task ExecuteOperationAsync<TState>(Guid correlationId, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await _outboxDataProvider.ExecuteActionInTransaction(correlationId, this, state, action, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, DateTime? startAtUtc, CancellationToken cancellationToken)
            where T : IOutboxMessage
        {
            var messageType = message.GetType();
            if (messageType != typeof(EmptyOutboxMessage) && message.MessageId == default)
                throw new InvalidOperationException("MessageId can't be empty");

            var assemblyQualifiedName = messageType.AssemblyQualifiedName;
            if (assemblyQualifiedName == null) throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            if (!_discriminator.TryGetDiscriminator(assemblyQualifiedName, out var discriminator))
            {
                throw new DiscriminatorResolveTypeException("Type discriminator not found");
            }

            var envelopeId = message.MessageId;
            var msgBody = _serializer.Serialize(messageType, message);
            var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId, discriminator, OutboxMessageStatus.New, msgBody, startAtUtc);
            await _outboxDataProvider.Add(outboxEnvelope, cancellationToken).ConfigureAwait(false);

            return message.MessageId;
        }

        public async Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return await _outboxDataProvider.IsExists(correlationId, cancellationToken).ConfigureAwait(false);
        }
    }
}