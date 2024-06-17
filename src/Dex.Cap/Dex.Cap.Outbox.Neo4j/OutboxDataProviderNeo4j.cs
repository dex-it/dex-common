using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.Options;
using Neo4jClient.Transactions;

namespace Dex.Cap.Outbox.Neo4j
{
    // ReSharper disable once InconsistentNaming
    internal sealed class OutboxDataProviderNeo4j(
        ITransactionalGraphClient graphClient,
        IOptions<OutboxOptions> outboxOptions,
        IOutboxRetryStrategy retryStrategy)
        : BaseOutboxDataProvider<ITransactionalGraphClient>(retryStrategy)
    {
        private readonly ITransactionalGraphClient _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        private readonly OutboxOptions _outboxOptions = outboxOptions?.Value ?? throw new ArgumentNullException(nameof(outboxOptions));

        public override Task ExecuteActionInTransaction<TState>(Guid correlationId, IOutboxService<ITransactionalGraphClient> outboxService, TState state,
            Func<CancellationToken, IOutboxContext<ITransactionalGraphClient, TState>, Task> action, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            await _graphClient.Cypher
                .Create($"(outbox:{nameof(Outbox)})")
                .Set("outbox = {outbox}").WithParam("outbox", outboxEnvelope)
                .ExecuteWithoutResultsAsync();

            return outboxEnvelope;
        }

        public override Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task<IOutboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken)
        {
            var outboxes = await GetFreeMessages(cancellationToken);
            return outboxes.Select(x => (IOutboxLockedJob)new OutboxLockedJob(x)).ToArray();
        }

        /// <exception cref="OperationCanceledException"/>
        public override async Task<OutboxEnvelope[]> GetFreeMessages(CancellationToken cancellationToken)
        {
            const OutboxMessageStatus failedStatus = OutboxMessageStatus.Failed;
            const OutboxMessageStatus newStatus = OutboxMessageStatus.New;

            var potentialFree = await _graphClient.Cypher.Match($"(o:{nameof(Outbox)})")
                .Where((OutboxEnvelope o) => o.Retries < _outboxOptions.Retries)
                .AndWhere((OutboxEnvelope o) => o.Status == failedStatus || o.Status == newStatus)
                .Return<OutboxEnvelope>("o")
                .OrderBy("o.Created")
                .Limit(_outboxOptions.MessagesToProcess)
                .ResultsAsync;

            return potentialFree.ToArray();
        }

        public override int GetFreeMessagesCount()
        {
            throw new NotImplementedException();
        }

        protected override async Task CompleteJobAsync(IOutboxLockedJob outboxEnvelope, CancellationToken cancellationToken)
        {
            await _graphClient.Cypher.Merge($"(o:{nameof(Outbox)} {{ Id: {{id}} }})")
                .WithParam("id", outboxEnvelope.Envelope.Id)
                .Set("o = {outbox}").WithParam("outbox", outboxEnvelope)
                .ExecuteWithoutResultsAsync();
        }
    }
}