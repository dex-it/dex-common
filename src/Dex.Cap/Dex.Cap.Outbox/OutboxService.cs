using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public class OutboxService : IOutboxService
    {
        private readonly IOutboxDataProvider _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<Guid> ExecuteOperation<T>(Guid correlationId, Func<CancellationToken, Task> operation, T message, CancellationToken cancellationToken)
            where T : IOutboxMessage
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            await _outboxDataProvider.ExecuteInTransaction(correlationId, async token =>
            {
                await operation(token);
                await Enqueue(correlationId, message, token);
            }, cancellationToken);
            return correlationId;
        }

        public async Task<Guid> Enqueue<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            var assemblyQualifiedName = message.GetType().AssemblyQualifiedName;
            if (assemblyQualifiedName == null)
                throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            var outbox = new OutboxEnvelope(correlationId, assemblyQualifiedName, OutboxMessageStatus.New, _serializer.Serialize(message));
            await _outboxDataProvider.Add(outbox, cancellationToken);
            return correlationId;
        }

        public Task<Guid> Enqueue<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            return Enqueue(Guid.NewGuid(), message, cancellationToken);
        }

        public Task<bool> IsOperationExists(Guid correlationId, CancellationToken cancellationToken)
        {
            return _outboxDataProvider.IsExists(correlationId, cancellationToken);
        }
    }
}