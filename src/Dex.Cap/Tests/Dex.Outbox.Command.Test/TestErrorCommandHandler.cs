﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorCommandHandler : IOutboxMessageHandler<TestErrorOutboxCommand>
    {
        public static event EventHandler OnProcess;
        private static int _count;

        public Task ProcessMessage(TestErrorOutboxCommand message, CancellationToken cancellationToken)
        {
            _count++;

            Console.WriteLine($"Try processed command at {DateTime.Now}, Count: {message.CountDown}");

            if (message.CountDown > _count) throw new InvalidDataException();

            Console.WriteLine($"Processed command at {DateTime.Now}, Count: {message.CountDown}");

            OnProcess?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestErrorOutboxCommand) outbox, cancellationToken);
        }
    }


}