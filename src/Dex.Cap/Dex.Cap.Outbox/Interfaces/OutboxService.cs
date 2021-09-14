using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService : IOutboxService
    {
        private readonly IOutboxDataProvider _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<Guid> ExecuteOperationAsync<T>(Guid correlationId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            where T : IOutboxMessage
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            await _outboxDataProvider.ExecuteInTransactionAsync(correlationId,
                async token => await EnqueueAsync(correlationId, await operation(token).ConfigureAwait(false), token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            return correlationId;
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            var assemblyQualifiedName = message.GetType().AssemblyQualifiedName;
            if (assemblyQualifiedName == null)
                throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            var outbox = new OutboxEnvelope(correlationId, assemblyQualifiedName, OutboxMessageStatus.New, _serializer.Serialize(message));
            await _outboxDataProvider.AddAsync(outbox, cancellationToken).ConfigureAwait(false);
            return correlationId;
        }

        public Task<Guid> EnqueueAsync<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            return EnqueueAsync(Guid.NewGuid(), message, cancellationToken);
        }

        public Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return _outboxDataProvider.IsExistsAsync(correlationId, cancellationToken);
        }
    }
}