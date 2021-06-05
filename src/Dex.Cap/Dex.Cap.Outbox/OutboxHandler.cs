using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox
{
    public class OutboxHandler<TDbContext> : IOutboxHandler
    {
        private readonly IOutboxDataProvider<TDbContext> _dataProvider;
        private readonly IOutboxMessageHandlerFactory _handlerFactory;
        private readonly IOutboxSerializer _serializer;
        private readonly ILogger<OutboxHandler<TDbContext>> _logger;

        public OutboxHandler(IOutboxDataProvider<TDbContext> dataProvider, IOutboxMessageHandlerFactory messageHandlerFactory,
            IOutboxSerializer serializer, ILogger<OutboxHandler<TDbContext>> logger)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _handlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process()
        {
            _logger.LogDebug("Outbox processor has been started");

            var messages = await _dataProvider.GetWaitingMessages();
            _logger.LogDebug("Messages to process {Count}", messages.Length);

            foreach (var message in messages)
            {
                _logger.LogDebug("Message has been started to process {MessageId}", message.Id);

                try
                {
                    var messageType = Type.GetType(message.MessageType);
                    if (messageType == null)
                    {
                        throw new InvalidOperationException($"Can't resolve type of message, {message.MessageType}");
                    }

                    var msg = _serializer.Deserialize(messageType, message.Content);
                    _logger.LogDebug("Message to processed: {Message}", msg);

                    if (msg is IOutboxMessage outboxMessage)
                    {
                        var handler = _handlerFactory.GetMessageHandler(outboxMessage);
                        try
                        {
                            await handler.ProcessMessage(outboxMessage);
                        }
                        finally
                        {
                            // ReSharper disable once SuspiciousTypeConversion.Global
                            (handler as IDisposable)?.Dispose();
                        }

                        await _dataProvider.Succeed(message);
                    }
                    else
                    {
                        message.Retries = int.MaxValue;
                        throw new InvalidOperationException("Message are not of IOutboxMessage type");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to process {MessageId}", message.Id);
                    await _dataProvider.Fail(message, e.Message, e);
                }

                _logger.LogDebug("Message {MessageId} has been processed", message.Id);
            }

            _logger.LogDebug("Outbox processor finished");
        }
    }
}