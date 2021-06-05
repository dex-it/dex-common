using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox;

namespace Dex.Outbox.Command.Test
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        public Task ProcessMessage(TestOutboxCommand message)
        {
            Console.WriteLine($"Processed command at {DateTime.Now}, Args: {message.Args}");
            return Task.CompletedTask;
        }

        public Task ProcessMessage(IOutboxMessage outbox)
        {
            return ProcessMessage((TestOutboxCommand) outbox);
        }
    }
}