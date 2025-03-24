﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxServiceExtensions
    {
        public static Task<Guid> EnqueueEventAsync(
            this IOutboxService<object?> outboxService,
            Guid correlationId,
            object outboxMessage,
            CancellationToken cancellationToken = default)
            => outboxService.EnqueueEventAsync<IBus>(correlationId, outboxMessage, cancellationToken);

        public static async Task<Guid> EnqueueEventAsync<TBus>(
            this IOutboxService<object?> outboxService,
            Guid correlationId,
            object outboxMessage,
            CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            if (outboxService == null) throw new ArgumentNullException(nameof(outboxService));

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage, outboxService.Discriminator);
            return await outboxService.EnqueueAsync(correlationId, eventMessage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}