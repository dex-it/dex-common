using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Events.Distributed.Tests.Events;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestOnCardAddedHandler : IDistributedEventHandler<OnCardAdded>
    {
        public static event EventHandler OnProcessed;

        public Task ProcessAsync(OnCardAdded argument, CancellationToken cancellationToken)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            Console.WriteLine($"{nameof(TestOnCardAddedHandler)} - Processed command at {DateTime.Now}, Args: {argument.CustomerId}");
            OnProcessed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }

    public class TestOnCardAddedHandler2 : IDistributedEventHandler<OnCardAdded>
    {
        public static event EventHandler OnProcessed;

        public Task ProcessAsync(OnCardAdded argument, CancellationToken cancellationToken)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            Console.WriteLine($"{nameof(TestOnCardAddedHandler2)} - Processed command at {DateTime.Now}, Args: {argument.CustomerId}");

            OnProcessed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}