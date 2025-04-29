using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorCommandHandler : IOutboxMessageHandler<TestErrorOutboxCommand>
    {
        public static event EventHandler OnProcess;
        public static void Reset() => Count = 0;

        private static int Count { get; set; }

        public Task Process(TestErrorOutboxCommand message, CancellationToken cancellationToken)
        {
            Count++;

            Console.WriteLine($"TestErrorCommandHandler - Try processed command at {DateTime.Now}, MaxCount: {message.MaxCount}");

            if (message.MaxCount > Count) throw new InvalidDataException();

            Console.WriteLine($"TestErrorCommandHandler - Processed command at {DateTime.Now}, Count: {message.MaxCount}");

            OnProcess?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}