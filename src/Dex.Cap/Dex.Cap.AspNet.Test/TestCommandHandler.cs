using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
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

    public class TestOutboxCommand : BaseOutboxMessage
    {
        public string Args { get; set; }
    }
}