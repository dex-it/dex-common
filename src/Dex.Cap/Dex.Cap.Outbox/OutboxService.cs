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

        public async Task<Guid> Enqueue<T>(T message, Guid correlationId, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            var assemblyQualifiedName = message.GetType().AssemblyQualifiedName;
            if (assemblyQualifiedName == null)
                throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            var outbox = new OutboxEnvelope(correlationId, assemblyQualifiedName, OutboxMessageStatus.New, _serializer.Serialize(message));
            await _outboxDataProvider.Save(outbox, cancellationToken);
            return correlationId;
        }

        public Task<Guid> Enqueue<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            return Enqueue(message, Guid.NewGuid(), cancellationToken);
        }

        public Task<bool> IsOperationExists(Guid correlationId, CancellationToken cancellationToken)
        {
            return _outboxDataProvider.IsExists(correlationId, cancellationToken);
        }
    }
}