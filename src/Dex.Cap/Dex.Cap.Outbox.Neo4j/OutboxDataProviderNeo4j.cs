using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    internal class OutboxDataProviderNeo4j : BaseOutboxDataProvider<ITransactionalGraphClient>
    {
        private readonly ITransactionalGraphClient _graphClient;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderNeo4j(ITransactionalGraphClient graphClient, IOptions<OutboxOptions> outboxOptions)
        {
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _outboxOptions = outboxOptions?.Value ?? throw new ArgumentNullException(nameof(outboxOptions));
        }

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

        public override async IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var outboxes = await GetFreeMessages(_outboxOptions.MessagesToProcess, cancellationToken);

            foreach (var o in outboxes)
            {
                yield return new OutboxLockedJob(o, default, cts: null);
            }
        }

        protected override async Task CompleteJobAsync(IOutboxLockedJob outboxEnvelope, CancellationToken cancellationToken)
        {
            await _graphClient.Cypher.Merge($"(o:{nameof(Outbox)} {{ Id: {{id}} }})")
                .WithParam("id", outboxEnvelope.Envelope.Id)
                .Set("o = {outbox}").WithParam("outbox", outboxEnvelope)
                .ExecuteWithoutResultsAsync();
        }

        /// <exception cref="OperationCanceledException"/>
        public override async Task<OutboxEnvelope[]> GetFreeMessages(int limit, CancellationToken cancellationToken)
        {
            const OutboxMessageStatus failedStatus = OutboxMessageStatus.Failed;
            const OutboxMessageStatus newStatus = OutboxMessageStatus.New;

            var potentialFree = await _graphClient.Cypher.Match($"(o:{nameof(Outbox)})")
                .Where((OutboxEnvelope o) => o.Retries < _outboxOptions.Retries)
                .AndWhere((OutboxEnvelope o) => o.Status == failedStatus || o.Status == newStatus)
                .Return<OutboxEnvelope>("o")
                .OrderBy("o.Created")
                .Limit(limit)
                .ResultsAsync;

            return potentialFree.ToArray();
        }
    }
}