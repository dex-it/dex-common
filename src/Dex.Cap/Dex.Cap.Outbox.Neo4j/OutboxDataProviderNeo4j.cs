using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.Options;
using Neo4jClient.Transactions;

namespace Dex.Cap.Outbox.Neo4j
{
    // ReSharper disable once InconsistentNaming
    public class OutboxDataProviderNeo4j : BaseOutboxDataProvider
    {
        private readonly ITransactionalGraphClient _graphClient;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderNeo4j(ITransactionalGraphClient graphClient, IOptions<OutboxOptions> outboxOptions)
        {
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _outboxOptions = outboxOptions?.Value ?? throw new ArgumentNullException(nameof(outboxOptions));
        }

        public override async Task<OutboxEnvelope> Save(OutboxEnvelope outboxEnvelope)
        {
            await _graphClient.Cypher
                .Create($"(outbox:{nameof(Outbox)})")
                .Set("outbox = {outbox}").WithParam("outbox", outboxEnvelope)
                .ExecuteWithoutResultsAsync();

            return outboxEnvelope;
        }

        public override async Task<OutboxEnvelope[]> GetWaitingMessages()
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

            return outboxes.ToArray();
        }

        protected override async Task UpdateOutbox(OutboxEnvelope outboxEnvelope)
        {
            await _graphClient.Cypher.Merge($"(o:{nameof(Outbox)} {{ Id: {{id}} }})")
                .WithParam("id", outboxEnvelope.Id)
                .Set("o = {outbox}").WithParam("outbox", outboxEnvelope)
                .ExecuteWithoutResultsAsync();
        }
    }
}