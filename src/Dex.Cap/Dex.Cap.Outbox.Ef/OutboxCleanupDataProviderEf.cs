using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Ef
{
    internal sealed class OutboxCleanupDataProviderEf<TDbContext> : BaseCleanupProvider where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ILogger _logger;

        public OutboxCleanupDataProviderEf(TDbContext dbContext, ILogger<OutboxCleanupDataProviderEf<TDbContext>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            const int status = (int)OutboxMessageStatus.Succeeded;
            const int limit = 1000;
            var total = 0;

            var stamp = DateTime.UtcNow.Subtract(olderThan);

            var schema = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite" ? string.Empty : "cap.";
            
            var sql = $"delete from {schema}outbox where \"Id\" in (select \"Id\" from {schema}outbox " +
                      $"where {schema}outbox.\"CreatedUtc\" < '{stamp:u}' and \"Status\" = {status} order by \"CreatedUtc\" limit {limit})";

            _logger.LogDebug("SQL: {DeleteSqlCommandText}", sql);

            var cont = true;
            while (cont)
            {
                var affected = await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
                cont = affected > 0;
                total += affected;
            }

            return total;
        }
    }
}