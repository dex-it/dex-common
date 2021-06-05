using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox;

namespace Dex.Outbox.Command.Test
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        public static event EventHandler OnProcess;

        public Task ProcessMessage(TestOutboxCommand message)
        {
            Console.WriteLine($"Processed command at {DateTime.Now}, Args: {message.Args}");
            OnProcess?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task ProcessMessage(IOutboxMessage outbox)
        {
            return ProcessMessage((TestOutboxCommand) outbox);
        }
    }
}