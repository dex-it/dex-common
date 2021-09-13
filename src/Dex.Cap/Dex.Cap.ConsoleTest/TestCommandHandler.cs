using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.ConsoleTest
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        public static event EventHandler OnProcess;

        public async Task ProcessMessage(TestOutboxCommand message, CancellationToken cancellationToken)
        {
            //Console.WriteLine($"TestCommandHandler - Processing command at {DateTime.Now}, Args: {message.Args}");

            await Task.Delay(2_000, cancellationToken);

            Console.WriteLine($"TestCommandHandler - Processed command at {DateTime.Now}, Args: {message.Args}");

            OnProcess?.Invoke(this, EventArgs.Empty);
        }

        [DebuggerStepThrough]
        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestOutboxCommand)outbox, cancellationToken);
        }
    }
}
