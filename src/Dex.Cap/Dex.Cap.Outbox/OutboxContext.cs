using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    internal class OutboxContext<TDbContext, TState> : IOutboxContext<TDbContext, TState>
    {
        public TDbContext DbContext { get; }
        public TState State { get; }
        private IOutboxService<TDbContext> OutboxService { get; }

        public OutboxContext(IOutboxService<TDbContext> outboxService, TDbContext dbContext, TState state)
        {
            OutboxService = outboxService;
            DbContext = dbContext;
            State = state;
        }

        public async Task AddCommandAsync(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            await OutboxService.EnqueueAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}