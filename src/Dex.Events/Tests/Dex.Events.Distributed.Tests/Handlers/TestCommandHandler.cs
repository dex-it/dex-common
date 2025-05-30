﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public class TestCommandHandler : IOutboxMessageHandler<TestOutboxCommand>
    {
        public static event EventHandler<TestOutboxCommand> OnProcess;

        public Task Process(TestOutboxCommand message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{nameof(TestCommandHandler)} - Processed command at {DateTime.Now}, Args: {message.Args}");

            OnProcess?.Invoke(null, message);
            return Task.CompletedTask;
        }
    }
}