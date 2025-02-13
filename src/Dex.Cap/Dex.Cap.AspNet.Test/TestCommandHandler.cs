using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.AspNet.Test
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        private readonly ILogger<TestCommandHandler> _logger;
        public static event EventHandler OnProcess;

        public TestCommandHandler(ILogger<TestCommandHandler> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessage(TestOutboxCommand message, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);

            _logger.LogInformation("TestCommandHandler - Processed command at {Now}, Args: {MessageArgs}", DateTime.Now, message.Args);

            OnProcess?.Invoke(this, EventArgs.Empty);
        }

        [DebuggerStepThrough]
        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestOutboxCommand)outbox, cancellationToken);
        }
    }

    public class TestOutboxCommand : IOutboxMessage
    {
        public string Args { get; set; }
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}