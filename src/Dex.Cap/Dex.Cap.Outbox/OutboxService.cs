using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public class OutboxService<TDbContext> : IOutboxService<TDbContext>
    {
        private readonly IOutboxDataProvider<TDbContext> _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider<TDbContext> outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public Task Publish<T>(T message, Guid correlationId)
        {
            return InnerSend(message, correlationId, OutboxMessageType.Event);
        }

        public Task Send<T>(T message, Guid correlationId)
        {
            return InnerSend(message, correlationId, OutboxMessageType.Command);
        }

        private Task InnerSend<T>(T message, Guid correlationId, OutboxMessageType messageType)
        {
            var outbox = new Models.Outbox
            {
                CorrelationId = correlationId,
                Name = message.GetType().Name,
                Status = OutboxMessageStatus.New,
                OutboxMessageType = messageType,
                Content = _serializer.Serialize(message)
            };

            return _outboxDataProvider.Save(outbox);
        }
    }
}