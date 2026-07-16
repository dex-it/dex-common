using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxCleanupDataProviderEf<TDbContext>(
    TDbContext dbContext,
    IInboxTypeDiscriminatorProvider discriminatorProvider,
    IOptions<InboxHandlerOptions> options,
    ILogger<InboxCleanupDataProviderEf<TDbContext>> logger)
    : IInboxCleanupDataProvider
    where TDbContext : DbContext
{
    private readonly int _batchSize = options.Value.CleanupBatchSize;
    private readonly TimeSpan _batchDelay = options.Value.CleanupBatchDelay;

    /// <inheritdoc />
    /// <remarks>
    /// Удаляет только сообщения ЭТОГО сервиса. Одну таблицу могут обслуживать несколько сервисов, у
    /// каждого свой набор дискриминаторов и свой ретеншен. Ретеншен здесь это окно дедупликации, поэтому
    /// сервис с недельным ретеншеном, вычистив чужие строки, молча укоротил бы окно соседа и тот начал бы
    /// принимать повторы как новые сообщения. Тот же фильтр стоит в выборке и в метриках.
    /// <para>
    /// Удаляет пачками, пока очередная пачка не придёт пустой. Неполная пачка не означает, что чистить
    /// больше нечего: строку мог увести на новый адрес конкурентный писатель или занять соседняя реплика.
    /// Выход по неполной пачке бросил бы оставшийся хвост до следующего запуска, то есть на час.
    /// </para>
    /// <para>
    /// Цикл конечен при любом темпе приёма сообщений: граница возраста вычисляется до цикла, поэтому строки с
    /// подходящим CreatedUtc образуют фиксированное конечное множество, и каждая удаляется однократно. Само
    /// множество подходящих под удаление строк при этом может и пополняться, когда старое необработанное
    /// сообщение становится Succeeded прямо во время цикла, но пополняться ему неоткуда, кроме этого же
    /// конечного набора.
    /// </para>
    /// </remarks>
    public async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        dbContext.Database.EnsureNpgsql();

        var ownDiscriminators = discriminatorProvider.SupportedDiscriminators;

        if (ownDiscriminators.Count is 0)
        {
            LogSkippedBecauseServiceOwnsNothing();
            return 0;
        }

        var createdBefore = DateTime.UtcNow.Subtract(olderThan);
        var sql = BuildDeleteBatchSql(_batchSize);
        var total = 0;
        int removed;

        logger.LogDebug("Cleaning up inbox messages older than {Timestamp} with status {Status}",
            createdBefore, InboxMessageStatus.Succeeded);

        do
        {
            removed = await DeleteBatchAsync(sql, createdBefore, ownDiscriminators, cancellationToken).ConfigureAwait(false);
            total += removed;

            // Пауза только между непустыми пачками: размазать первый большой проход, не задерживая выход из цикла.
            if (_batchDelay > TimeSpan.Zero && removed > 0)
            {
                await Task.Delay(_batchDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        while (removed > 0);

        return total;
    }

    /// <summary>
    /// Удалить одну пачку обработанных сообщений этого сервиса.
    /// </summary>
    /// <returns>Количество удалённых строк; ноль означает, что подходящих строк не осталось.</returns>
    private async Task<int> DeleteBatchAsync(
        string sql,
        DateTime createdBefore,
        IReadOnlyCollection<string> ownDiscriminators,
        CancellationToken cancellationToken)
    {
        var parameters = new object[] { createdBefore, ownDiscriminators.ToArray() };

        return await dbContext.Database
            .ExecuteSqlRawAsync(sql, parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Собрать запрос удаления одной пачки: только обработанные сообщения этого сервиса старше границы.
    /// </summary>
    /// <remarks>
    /// Строки адресуются по ctid, а не по первичному ключу. LINQ-вариант EF превращает в
    /// <c>DELETE ... WHERE "Id" IN (SELECT ...)</c>, на котором планировщик берёт Seq Scan с Hash Semi
    /// Join: индекс отбора работает, но всю таблицу приходится читать на каждую пачку. Удаление по списку
    /// ключей ещё хуже: тысяча случайных uuid по btree дороже последовательного чтения. Адрес строки
    /// безопасен здесь потому, что подзапрос и удаление это один стейтмент с одним снимком, а FOR UPDATE
    /// удерживает адрес до конца стейтмента.
    /// <para>
    /// SKIP LOCKED обязателен: чистильщик живёт на каждой реплике, и без него реплики выбирают одни и те
    /// же строки. Проигравший ждал бы победителя (а с ним и любого чужого писателя), после чего получал
    /// бы пустой результат и считал, что чистить нечего. Занятая строка пропускается и удаляется
    /// следующим заходом.
    /// </para>
    /// <para>
    /// Без ORDER BY: порядок удаления внутри вызова ни на что не влияет, потому что цикл выгребает все
    /// подходящие строки. Сортировка по одному <see cref="InboxEnvelope.CreatedUtc"/> при нескольких
    /// дискриминаторах не берётся из индекса и уводит планировщик в Seq Scan. Индекс
    /// (Status, MessageType, CreatedUtc) и так отдаёт старейшие первыми внутри каждого дискриминатора.
    /// </para>
    /// <para>
    /// Удаляются только <see cref="InboxMessageStatus.Succeeded"/>. Строки
    /// <see cref="InboxMessageStatus.DeadLettered"/> требуют ручного разбора, и стирать их значило бы
    /// прятать инциденты.
    /// </para>
    /// </remarks>
    private static string BuildDeleteBatchSql(int batchSize) =>
        $$"""
          DELETE FROM {{NameConst.SchemaName}}.{{NameConst.TableName}}
          WHERE ctid IN (
              SELECT ctid
              FROM {{NameConst.SchemaName}}.{{NameConst.TableName}}
              WHERE "{{nameof(InboxEnvelope.Status)}}" = {{InboxMessageStatus.Succeeded:D}}
                AND "{{nameof(InboxEnvelope.CreatedUtc)}}" < {0}
                AND "{{nameof(InboxEnvelope.MessageType)}}" = ANY({1})
              LIMIT {{batchSize}}
              FOR UPDATE SKIP LOCKED
          )
          """;

    /// <summary>
    /// Сообщить, что чистка не рассматривала ни одной строки.
    /// </summary>
    /// <remarks>
    /// Сервис без обработчиков не владеет ни одним сообщением, и «удалено 0» на вызывающей стороне
    /// неотличимо от «чистить было нечего». Строки типов, у которых в этом сервисе не осталось
    /// обработчика, не удалит уже никто, поэтому факт пропуска обязан быть виден в логе.
    /// </remarks>
    private void LogSkippedBecauseServiceOwnsNothing() =>
        logger.LogWarning("Inbox cleanup skipped: the service has no message handlers, so it owns no messages to clean up");
}