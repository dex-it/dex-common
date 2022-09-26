using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    internal class OutboxContext<TState> : IOutboxContext<TState>
    {
        public TState State { get; }
        
        private IOutboxService OutboxService { get; }
        
        public OutboxContext(IOutboxService outboxService, TState state)
        {
            OutboxService = outboxService;
            State = state;
        }

        public async Task AddCommandAsync(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            await OutboxService.EnqueueAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}