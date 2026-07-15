using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxCleanupDataProviderEf<TDbContext>(TDbContext dbContext, ILogger<InboxCleanupDataProviderEf<TDbContext>> logger)
    : IInboxCleanupDataProvider
    where TDbContext : DbContext
{
    private const int Limit = 1000;

    public async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        EnsureNpgsql();

        var stamp = DateTime.UtcNow.Subtract(olderThan);
        var total = 0;
        int affected;

        logger.LogDebug("Cleaning up inbox messages older than {Timestamp} with status {Status}", stamp, InboxMessageStatus.Succeeded);

        // Удаляем по физическому адресу строки, а не по первичному ключу. LINQ-вариант
        // (Where(...).Take(...).ExecuteDelete()) EF превращает в DELETE ... WHERE "Id" IN (SELECT ...),
        // и планировщик берёт на внешнем удалении Seq Scan с Hash Semi Join: индекс отбора работает,
        // но всю таблицу приходится читать на КАЖДУЮ пачку. Замер на 210 тыс. строк: 3294 буфера и
        // 27.5 мс против 2013 буферов и 1.9 мс у варианта ниже. Удаление по списку ключей ещё хуже
        // (4011 буферов): тысяча случайных uuid по btree дороже последовательного чтения.
        //
        // ctid безопасен именно здесь: подзапрос и удаление это ОДИН стейтмент с одним снимком, а
        // адрес строки меняется только при её обновлении. Succeeded терминален, выборка его не берёт
        // (ScheduledStartIndexing IS NULL), поэтому обновлять эти строки некому.
        //
        // Удаляем только Succeeded. DeadLettered остаётся: это сообщения, требующие ручного разбора,
        // и молча стирать их означало бы прятать инциденты.
        var sql = $"""
                   DELETE FROM {NameConst.SchemaName}.{NameConst.TableName}
                   WHERE ctid IN (
                       SELECT ctid
                       FROM {NameConst.SchemaName}.{NameConst.TableName}
                       WHERE "{nameof(InboxEnvelope.Status)}" = {InboxMessageStatus.Succeeded:D}
                         AND "{nameof(InboxEnvelope.CreatedUtc)}" < @p0
                       ORDER BY "{nameof(InboxEnvelope.CreatedUtc)}"
                       LIMIT {Limit}
                   )
                   """;

        do
        {
            affected = await dbContext.Database
                .ExecuteSqlRawAsync(sql, [stamp], cancellationToken)
                .ConfigureAwait(false);

            total += affected;
        }
        while (affected == Limit);

        return total;
    }

    private void EnsureNpgsql()
    {
        var providerName = dbContext.Database.ProviderName;

        if (providerName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
            throw new NotSupportedException($"The provider {providerName} is not supported.");
    }
}
