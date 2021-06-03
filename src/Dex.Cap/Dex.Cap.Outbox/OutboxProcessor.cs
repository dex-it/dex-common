using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox
{
    public class OutboxProcessor<TDbContext> : IOutboxProcessor
    {
        private readonly string[] _loggerCategory = {"OutboxProcessor"};

        private readonly IOutboxDataProvider<TDbContext> _dataProvider;
        private readonly IOutboxMessageProcessorFactory _messageProcessorFactory;
        private readonly ILogger<OutboxProcessor<TDbContext>> _logger;

        public OutboxProcessor(IOutboxDataProvider<TDbContext> dataProvider, IOutboxMessageProcessorFactory messageProcessorFactory, ILogger<OutboxProcessor<TDbContext>> logger)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _messageProcessorFactory = messageProcessorFactory ?? throw new ArgumentNullException(nameof(messageProcessorFactory));
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

                var messageProcessor = _messageProcessorFactory.GetMessageProcessor(message);
                try
                {
                    await messageProcessor.ProcessMessage(message);
                    await _dataProvider.Succeed(message);
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