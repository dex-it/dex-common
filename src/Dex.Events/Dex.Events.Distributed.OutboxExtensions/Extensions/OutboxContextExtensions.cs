﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxContextExtensions
    {
        public static Task EnqueueEventAsync(
            this IOutboxContext<object?, object?> outboxContext,
            IDistributedEventParams outboxMessage,
            CancellationToken cancellationToken = default)
            => outboxContext.EnqueueEventAsync<IBus>(outboxMessage, cancellationToken);

        public static async Task EnqueueEventAsync<TBus>(
            this IOutboxContext<object?, object?> outboxContext,
            IDistributedEventParams outboxMessage,
            CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            ArgumentNullException.ThrowIfNull(outboxContext);

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage, outboxContext.GetDiscriminator());
            await outboxContext.EnqueueAsync(eventMessage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}