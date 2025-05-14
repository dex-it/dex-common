using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestCommand2Handler : IOutboxMessageHandler<TestOutboxCommand2>
    {
        public static event EventHandler<TestOutboxCommand2> OnProcess;

        public Task Process(TestOutboxCommand2 message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestCommand2Handler - Processed command at {DateTime.Now}, Args: {message.Args}");
            OnProcess?.Invoke(this, message);
            return Task.CompletedTask;
        }
    }
}