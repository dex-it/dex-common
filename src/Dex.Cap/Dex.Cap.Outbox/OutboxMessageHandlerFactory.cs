using System;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    public class OutboxMessageHandlerFactory : IOutboxMessageHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OutboxMessageHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IOutboxMessageHandler GetMessageHandler(IOutboxMessage outboxMessage)
        {
            if (outboxMessage == null)
            {
                throw new ArgumentNullException(nameof(outboxMessage));
            }

            var type = outboxMessage.GetType();
            var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(type);

            if (_serviceProvider.GetService(handlerType) is IOutboxMessageHandler handler)
            {
                return handler;
            }

            throw new OutboxException($"Can't resolve message handler for '{handlerType}'");
        }
    }
}