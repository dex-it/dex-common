using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    internal class OutboxContext<TDbContext, TState> : IOutboxContext<TDbContext, TState>
    {
        public TDbContext DbContext { get; }
        public TState State { get; }

        private Guid CorrelationId { get; }
        private IOutboxService<TDbContext> OutboxService { get; }

        public OutboxContext(Guid correlationId, IOutboxService<TDbContext> outboxService, TDbContext dbContext, TState state)
        {
            CorrelationId = correlationId;
            OutboxService = outboxService;
            DbContext = dbContext;
            State = state;
        }

        public async Task EnqueueAsync(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            await OutboxService.EnqueueAsync(CorrelationId, outboxMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}