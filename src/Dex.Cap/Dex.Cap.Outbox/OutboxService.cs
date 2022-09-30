using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService<TDbContext> : IOutboxService<TDbContext>
    {
        private readonly IOutboxDataProvider<TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider<TDbContext> outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task ExecuteOperationAsync<TState>(Guid correlationId, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await _outboxDataProvider.ExecuteActionInTransaction(correlationId, this, state, action, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            var messageType = message.GetType();
            var assemblyQualifiedName = messageType.AssemblyQualifiedName;
            if (assemblyQualifiedName == null) throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            var outboxEnvelope = new OutboxEnvelope(Guid.NewGuid(), correlationId, assemblyQualifiedName, OutboxMessageStatus.New,
                _serializer.Serialize(messageType, message));
            await _outboxDataProvider.Add(outboxEnvelope, cancellationToken).ConfigureAwait(false);
            return correlationId;
        }

        public async Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return await _outboxDataProvider.IsExists(correlationId, cancellationToken).ConfigureAwait(false);
        }
    }
}