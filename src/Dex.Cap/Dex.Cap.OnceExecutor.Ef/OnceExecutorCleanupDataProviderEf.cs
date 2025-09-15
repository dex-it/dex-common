using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.Interfaces;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.OnceExecutor.Ef;

internal sealed class OnceExecutorCleanupDataProviderEf<TDbContext>(
    TDbContext dbContext,
    ILogger<OnceExecutorCleanupDataProviderEf<TDbContext>> logger) : IOnceExecutorCleanupDataProvider where TDbContext : DbContext
{
    public async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        const int limit = 1000;
        var total = 0;

        var stamp = DateTime.UtcNow.Subtract(olderThan);

        logger.LogDebug("Cleaning up last transactions older than {Timestamp}", stamp);

        var cont = true;
        while (cont)
        {
            var affected = await dbContext.Set<LastTransaction>()
                .Where(trx => trx.Created < stamp)
                .OrderBy(trx => trx.Created)
                .Take(limit)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            cont = affected > 0;
            total += affected;
        }

        return total;
    }
}