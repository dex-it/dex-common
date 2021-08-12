using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Dex.Cap.Outbox.QuartzHandler
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class OutboxHandlerJob : IJob
    {
        private readonly IOutboxHandler _outboxHandler;
        private readonly ILogger<OutboxHandlerJob> _logger;

        public OutboxHandlerJob(IOutboxHandler outboxHandler, ILogger<OutboxHandlerJob> logger)
        {
            _outboxHandler = outboxHandler ?? throw new ArgumentNullException(nameof(outboxHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return _outboxHandler.Process(context.CancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Outbox handler fail");
            }

            return Task.CompletedTask;
        }
    }
}