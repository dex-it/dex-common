using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public class OutboxService<TDbContext> : IOutboxService
    {
        private readonly IOutboxDataProvider<TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider<TDbContext> outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public Task Enqueue<T>(T message, Guid correlationId) where T : IOutboxMessage
        {
            var assemblyQualifiedName = message.GetType().AssemblyQualifiedName;
            if (assemblyQualifiedName == null)
                throw new InvalidOperationException("Can't resolve assemblyQualifiedName");

            var outbox = new Models.Outbox(correlationId, assemblyQualifiedName, OutboxMessageStatus.New, _serializer.Serialize(message));
            return _outboxDataProvider.Save(outbox);
        }
    }
}