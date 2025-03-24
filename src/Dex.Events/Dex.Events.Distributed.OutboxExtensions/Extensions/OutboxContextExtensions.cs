using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxContextExtensions
    {
        public static Task<Guid> EnqueueEventAsync(
            this IOutboxContext<object?, object?> outboxContext,
            object outboxMessage,
            CancellationToken cancellationToken = default)
            => outboxContext.EnqueueEventAsync<IBus>(outboxMessage, cancellationToken);

        public static async Task<Guid> EnqueueEventAsync<TBus>(
            this IOutboxContext<object?, object?> outboxContext,
            object outboxMessage,
            CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            if (outboxContext == null) throw new ArgumentNullException(nameof(outboxContext));

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage, outboxContext.GetDiscriminator());
            return await outboxContext.EnqueueAsync(eventMessage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}