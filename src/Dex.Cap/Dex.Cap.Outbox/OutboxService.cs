using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService : IOutboxService
    {
        public IOutboxTypeDiscriminator Discriminator { get; }
        public Guid CorrelationId { get; }


        private readonly IOutboxDataProvider _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(
            IOutboxDataProvider outboxDataProvider,
            IOutboxSerializer serializer,
            IOutboxTypeDiscriminator discriminator)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
            CorrelationId = Guid.NewGuid();
        }

        public async Task<Guid> EnqueueAsync<T>(T message, Guid? correlationId = null, DateTime? startAtUtc = null,
            TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
            where T : class
        {
            var messageType = message.GetType();
            var envelopeId = Guid.NewGuid();

            var msgBody = _serializer.Serialize(messageType, message);
            var discriminator = Discriminator.ResolveDiscriminator(messageType);
            var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId ?? CorrelationId, discriminator, msgBody,
                startAtUtc, lockTimeout);

            await _outboxDataProvider
                .Add(outboxEnvelope, cancellationToken)
                .ConfigureAwait(false);

            return envelopeId;
        }

        public Task<bool> IsOperationExistsAsync(Guid? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            return _outboxDataProvider.IsExists(correlationId ?? CorrelationId, cancellationToken);
        }
    }
}