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
        private static int _enterCount;

        public static int EnterCount => _enterCount;

        public async Task ProcessMessage(TestOutboxCommand message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _enterCount);

            Console.WriteLine($"TestCommandHandler [{message.MessageId}] - Processed command at {DateTime.Now}, Args: {message.Args}");

            var delay = TimeSpan.FromMilliseconds(_random.Next(10, 100));
            Console.WriteLine($"TestCommandHandler - delay {delay}");

            await Task.Delay(delay, cancellationToken);
            OnProcess?.Invoke(this, message);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestOutboxCommand)outbox, cancellationToken);
        }
    }
}