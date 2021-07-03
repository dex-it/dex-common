using System;
using System.Threading.Tasks;
using Quartz;

namespace Dex.Cap.Outbox.QuartzHandler
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class OutboxHandlerJob : IJob
    {
        private readonly IOutboxHandler _outboxHandler;

        public OutboxHandlerJob(IOutboxHandler outboxHandler)
        {
            _outboxHandler = outboxHandler ?? throw new ArgumentNullException(nameof(outboxHandler));
        }

        public Task Execute(IJobExecutionContext context)
        {
            return _outboxHandler.Process(context.CancellationToken);
        }
    }
}