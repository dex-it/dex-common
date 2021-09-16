using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestCommand2Handler : IOutboxMessageHandler<TestOutboxCommand2>
    {
        public static event EventHandler OnProcess;

        public Task ProcessMessage(TestOutboxCommand2 message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestCommand2Handler - Processed command at {DateTime.Now}, Args: {message.Args}");
            OnProcess?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestOutboxCommand2) outbox, cancellationToken);
        }
    }
}