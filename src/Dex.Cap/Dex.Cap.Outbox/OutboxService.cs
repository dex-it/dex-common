using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService<TOptions, TDbContext> : IOutboxService<TOptions, TDbContext>
        where TOptions : ITransactionOptions
    {
        public IOutboxTypeDiscriminator Discriminator { get; }

        private readonly IOutboxDataProvider<TOptions, TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(
            IOutboxDataProvider<TOptions, TDbContext> outboxDataProvider,
            IOutboxSerializer serializer,
            IOutboxTypeDiscriminator discriminator)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
        }

        public Task ExecuteOperationAsync<TState>(
            Guid correlationId, TState state,
            Func<IOutboxContext<TDbContext, TState>, CancellationToken, Task> action,
            TOptions? options = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            return _outboxDataProvider
                .ExecuteActionInTransaction(correlationId, this, state, action, options, cancellationToken);
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, DateTime? startAtUtc = null,
            TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
            where T : class
        {
            var messageType = message.GetType();
            var envelopeId = messageType == typeof(EmptyOutboxMessage)
                ? Guid.Empty
                : Guid.NewGuid();

            var msgBody = _serializer.Serialize(messageType, message);
            var discriminator = Discriminator.ResolveDiscriminator(messageType);
            var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId, discriminator, msgBody, startAtUtc,
                lockTimeout);

            await _outboxDataProvider
                .Add(outboxEnvelope, cancellationToken)
                .ConfigureAwait(false);

            return envelopeId;
        }

        public Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            return _outboxDataProvider.IsExists(correlationId, cancellationToken);
        }
    }
}