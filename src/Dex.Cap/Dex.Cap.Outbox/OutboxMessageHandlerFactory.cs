using System;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxMessageHandlerFactory : IOutboxMessageHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OutboxMessageHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public object GetMessageHandler(object outboxMessage)
        {
            ArgumentNullException.ThrowIfNull(outboxMessage);

            var messageType = outboxMessage.GetType();
            var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(messageType);
            var handler = _serviceProvider.GetService(handlerType);

            if (handler is not null)
            {
                return handler;
            }

            throw new OutboxException($"Can't resolve message handler for '{handlerType}'");
        }
    }
}