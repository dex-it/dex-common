using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        public static event EventHandler<TestOutboxCommand> OnProcess;
        private readonly Random _random = new((int)DateTime.UtcNow.Ticks);

        public async Task ProcessMessage(TestOutboxCommand message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestCommandHandler - Processed command at {DateTime.Now}, Args: {message.Args}");
            OnProcess?.Invoke(this, message);

            await Task.Delay(_random.Next(15), cancellationToken);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestOutboxCommand)outbox, cancellationToken);
        }
    }
}