using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.Options;
using Neo4jClient.Transactions;

namespace Dex.Cap.Outbox.Neo4j
{
    public class OutboxDataProviderNeo4j : BaseOutboxDataProvider
    {
        private readonly ITransactionalGraphClient _graphClient;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderNeo4j(ITransactionalGraphClient graphClient, IOptions<OutboxOptions> outboxOptions)
        {
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _outboxOptions = outboxOptions?.Value ?? throw new ArgumentNullException(nameof(outboxOptions));
        }

        public override async Task<Models.Outbox> Save(Models.Outbox outbox)
        {
            await _graphClient.Cypher
                .Create($"(outbox:{nameof(Outbox)})")
                .Set("outbox = {outbox}").WithParam("outbox", outbox)
                .ExecuteWithoutResultsAsync();

            return outbox;
        }

        public override async Task<Models.Outbox[]> GetWaitingMessages()
        {
            const OutboxMessageStatus failedStatus = OutboxMessageStatus.Failed;
            const OutboxMessageStatus newStatus = OutboxMessageStatus.New;

            var outboxes = await _graphClient.Cypher.Match($"(o:{nameof(Outbox)})")
                .Where((Models.Outbox o) => o.Retries < _outboxOptions.Retries)
                .AndWhere((Models.Outbox o) => o.Status == failedStatus || o.Status == newStatus)
                .Return<Models.Outbox>("o")
                .OrderBy("o.Created")
                .Limit(_outboxOptions.MessagesToProcess)
                .ResultsAsync;

            return outboxes.ToArray();
        }

        protected override async Task UpdateOutbox(Models.Outbox outbox)
        {
            await _graphClient.Cypher.Merge($"(o:{nameof(Outbox)} {{ Id: {{id}} }})")
                .WithParam("id", outbox.Id)
                .Set("o = {outbox}").WithParam("outbox", outbox)
                .ExecuteWithoutResultsAsync();
        }
    }
}