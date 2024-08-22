using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    internal sealed class OutboxContext<TDbContext, TState>(Guid correlationId, IOutboxService<TDbContext> outboxService, TDbContext dbContext, TState state)
        : IOutboxContext<TDbContext, TState>
    {
        public TDbContext DbContext { get; } = dbContext;
        public TState State { get; } = state;

        private Guid CorrelationId { get; } = correlationId;
        private IOutboxService<TDbContext> OutboxService { get; } = outboxService;

        public async Task EnqueueAsync(IOutboxMessage outboxMessage, DateTime? startAtUtc, CancellationToken cancellationToken)
        {
            await OutboxService.EnqueueAsync(CorrelationId, outboxMessage, startAtUtc, null, cancellationToken).ConfigureAwait(false);
        }

        public IOutboxTypeDiscriminator GetDiscriminator()
        {
            return OutboxService.Discriminator;
        }
    }
}