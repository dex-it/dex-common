using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.Options;
using Neo4jClient.Transactions;

namespace Dex.Cap.Outbox.Neo4j
{
    // ReSharper disable once InconsistentNaming
    internal class OutboxDataProviderNeo4j : BaseOutboxDataProvider
    {
        private readonly ITransactionalGraphClient _graphClient;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderNeo4j(ITransactionalGraphClient graphClient, IOptions<OutboxOptions> outboxOptions)
        {
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _outboxOptions = outboxOptions?.Value ?? throw new ArgumentNullException(nameof(outboxOptions));
        }

        // public override async Task ExecuteInTransaction(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        // {
        //     using (var transaction = _graphClient.BeginTransaction())
        //     {
        //         await operation(cancellationToken);
        //         await transaction.CommitAsync();
        //     }
        // }

        public override Task ExecuteUsefulAndSaveOutboxActionIntoTransaction<TContext, TOutboxMessage>(Guid correlationId, 
            Func<CancellationToken, Task<TContext>> usefulAction, 
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken)
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
            const OutboxMessageStatus failedStatus = OutboxMessageStatus.Failed;
            const OutboxMessageStatus newStatus = OutboxMessageStatus.New;

            var outboxes = await _graphClient.Cypher.Match($"(o:{nameof(Outbox)})")
                .Where((OutboxEnvelope o) => o.Retries < _outboxOptions.Retries)
                .AndWhere((OutboxEnvelope o) => o.Status == failedStatus || o.Status == newStatus)
                .Return<OutboxEnvelope>("o")
                .OrderBy("o.Created")
                .Limit(_outboxOptions.MessagesToProcess)
                .ResultsAsync;

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
    }
}