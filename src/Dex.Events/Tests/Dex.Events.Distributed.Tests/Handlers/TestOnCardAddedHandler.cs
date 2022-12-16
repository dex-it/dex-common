using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Events.Distributed.Tests.Events;
using MassTransit;

#pragma warning disable CS0162

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestOnUserAddedHandler : IDistributedEventHandler<OnUserAdded>
    {
        public static event EventHandler OnProcessed;

        public Task ProcessAsync(OnUserAdded argument, CancellationToken cancellationToken)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            Console.WriteLine($"{nameof(TestOnUserAddedHandler)} - Processed command at {DateTime.Now}, Args: {argument.CustomerId}");
            OnProcessed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<OnUserAdded> context)
        {
            return ProcessAsync(context.Message, context.CancellationToken);
        }
    }

    public class TestOnUserAddedHandler2 : IDistributedEventHandler<OnUserAdded>
    {
        public static event EventHandler OnProcessed;

        public Task ProcessAsync(OnUserAdded argument, CancellationToken cancellationToken)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            Console.WriteLine($"{nameof(TestOnUserAddedHandler2)} - Processed command at {DateTime.Now}, Args: {argument.CustomerId}");

            OnProcessed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<OnUserAdded> context)
        {
            return ProcessAsync(context.Message, context.CancellationToken);
        }
    }

    public class TestOnUserAddedHandlerRaiseException : IDistributedEventHandler<OnUserAdded>
    {
        public static event EventHandler OnProcessed;

        public Task ProcessAsync(OnUserAdded argument, CancellationToken cancellationToken)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            Console.WriteLine($"{nameof(TestOnUserAddedHandlerRaiseException)} - Processed command at {DateTime.Now}, Args: {argument.CustomerId}");
            throw new Exception("test Exception");

            // ReSharper disable once HeuristicUnreachableCode
            OnProcessed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<OnUserAdded> context)
        {
            return ProcessAsync(context.Message, context.CancellationToken);
        }
    }
}