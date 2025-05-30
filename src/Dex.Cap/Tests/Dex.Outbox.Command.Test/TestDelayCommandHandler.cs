﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test
{
    public class TestDelayCommandHandler : IOutboxMessageHandler<TestDelayOutboxCommand>
    {
        public static event EventHandler<TestDelayOutboxCommand> OnProcess;

        public async Task Process(TestDelayOutboxCommand message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestCommandHandler - Processed command at {DateTime.Now}, Args: {message.Args}");

            await Task.Delay(message.DelayMsec, cancellationToken);

            OnProcess?.Invoke(this, message);
        }
    }
}
