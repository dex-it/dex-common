using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxCleanupDataProviderEf<TDbContext>(
    TDbContext dbContext,
    IInboxTypeDiscriminatorProvider discriminatorProvider,
    ILogger<InboxCleanupDataProviderEf<TDbContext>> logger)
    : IInboxCleanupDataProvider
    where TDbContext : DbContext
{
    private const int Limit = 1000;

    public async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        EnsureNpgsql();

        // Чистим только СВОИ сообщения. Одну таблицу могут обслуживать несколько сервисов, у каждого
        // свой набор дискриминаторов и свой ретеншен. Ретеншен здесь это окно дедупликации, поэтому
        // сервис с cleanupDays: 1, вычистив чужие строки, молча укоротил бы окно соседа с 30 дней до
        // одного дня, и сосед начал бы принимать повторы как новые сообщения. Тот же фильтр стоит в
        // выборке и в метриках.
        var discriminators = discriminatorProvider.SupportedDiscriminators;

        if (discriminators.Count is 0)
        {
            // Не молча: без обработчиков чистка не смотрит НИ на одну строку, и «удалено 0» на вызывающей
            // стороне выглядит как «чистить было нечего». Строки типов, у которых в этом сервисе не осталось
            // обработчика, не удалит уже никто, поэтому факт пропуска обязан быть виден в логе.
            logger.LogWarning("Inbox cleanup skipped: the service has no message handlers, so it owns no messages to clean up");
            return 0;
        }

        var stamp = DateTime.UtcNow.Subtract(olderThan);
        var total = 0;
        int affected;

        logger.LogDebug("Cleaning up inbox messages older than {Timestamp} with status {Status}", stamp, InboxMessageStatus.Succeeded);

        // Удаляем по физическому адресу строки, а не по первичному ключу. LINQ-вариант
        // (Where(...).Take(...).ExecuteDelete()) EF превращает в DELETE ... WHERE "Id" IN (SELECT ...),
        // и планировщик берёт на внешнем удалении Seq Scan с Hash Semi Join: индекс отбора работает,
        // но всю таблицу приходится читать на КАЖДУЮ пачку (замер на 210 тыс. строк: на порядок
        // дороже по времени). Удаление по списку ключей ещё хуже: тысяча случайных uuid по btree
        // дороже последовательного чтения.
        //
        // ctid безопасен именно здесь: подзапрос и удаление это ОДИН стейтмент с одним снимком, а
        // FOR UPDATE ниже удерживает адрес строки до конца стейтмента.
        //
        // FOR UPDATE SKIP LOCKED, а не голый подзапрос: чистильщики работают на каждой реплике, и без
        // него они выбирают ОДНИ И ТЕ ЖЕ строки. Проигравший ждёт победителя, а потом получает пустой
        // результат (его строки уже удалены) и по условию выхода ниже считает, что чистить нечего.
        // Со SKIP LOCKED реплики берут непересекающиеся пачки и не ждут друг друга.
        //
        // Без ORDER BY. Сортировка по CreatedUtc не нужна (цикл выгребает ВСЕ подходящие строки, порядок
        // внутри вызова ни на что не влияет), а плану вредит: при нескольких своих дискриминаторах индекс
        // не даёт готового порядка по одному CreatedUtc, и планировщик уходит в Seq Scan со снятием
        // сортировки поверх. Индекс (Status, MessageType, CreatedUtc) и так отдаёт старейшие первыми
        // внутри каждого дискриминатора.
        //
        // Удаляем только Succeeded. DeadLettered остаётся: это сообщения, требующие ручного разбора,
        // и молча стирать их означало бы прятать инциденты.
        var sql = $$"""
                    DELETE FROM {{NameConst.SchemaName}}.{{NameConst.TableName}}
                    WHERE ctid IN (
                        SELECT ctid
                        FROM {{NameConst.SchemaName}}.{{NameConst.TableName}}
                        WHERE "{{nameof(InboxEnvelope.Status)}}" = {{InboxMessageStatus.Succeeded:D}}
                          AND "{{nameof(InboxEnvelope.CreatedUtc)}}" < {0}
                          AND "{{nameof(InboxEnvelope.MessageType)}}" = ANY({1})
                        LIMIT {{Limit}}
                        FOR UPDATE SKIP LOCKED
                    )
                    """;

        var supported = discriminators.ToArray();

        // Выходим по ПУСТОЙ пачке, а не по неполной. Неполная пачка не означает, что чистить больше нечего:
        // строку могли обновить (тогда EPQ уводит её на новый ctid) или её держит соседняя реплика (SKIP
        // LOCKED пропускает такие). Выход по неполной пачке бросал бы оставшийся хвост до следующего
        // запуска, то есть на час. Цикл конечен при любом темпе вставки: stamp зафиксирован ДО цикла,
        // поэтому множество подходящих строк только сокращается, а новые сообщения в него не попадают.
        do
        {
            affected = await dbContext.Database
                .ExecuteSqlRawAsync(sql, new object[] { stamp, supported }, cancellationToken)
                .ConfigureAwait(false);

            total += affected;
        }
        while (affected > 0);

        return total;
    }

    private void EnsureNpgsql()
    {
        var providerName = dbContext.Database.ProviderName;

        if (providerName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
            throw new NotSupportedException($"The provider {providerName} is not supported.");
    }
}
