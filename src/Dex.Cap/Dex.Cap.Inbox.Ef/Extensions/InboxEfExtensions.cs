using System;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dex.Cap.Inbox.Ef.Extensions;

/// <summary>
/// Подключение таблицы инбокса к модели EF Core.
/// </summary>
public static class InboxEfExtensions
{
    /// <summary>
    /// Сконфигурировать таблицу инбокса. Вызывается в OnModelCreating.
    /// </summary>
    public static void InboxModelCreating(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<InboxEnvelope>();

        AddDeduplicationKey(entity);
        AddFetchIndex(entity);
        AddOwnFetchIndex(entity);
        AddCleanupIndex(entity);

        entity
            .Property(x => x.LockTimeout)
            .HasDefaultValue(InboxEnvelope.DefaultLockTimeout)
            .HasComment("Maximum allowable blocking time");

        entity
            .Property(x => x.LockId)
            .HasComment("Idempotency key (unique key of the thread that captured the lock)");

        entity
            .Property(x => x.LockExpirationTimeUtc)
            .HasComment("Preventive timeout (maximum lifetime of actuality 'LockId')");
    }

    /// <summary>
    /// Ключ дедупликации: пара «идентификатор сообщения - потребитель».
    /// </summary>
    /// <remarks>
    /// Уникальность обязана быть на уровне БД: она обслуживает ON CONFLICT DO NOTHING при приёме
    /// сообщения и разруливает гонку конкурентных доставок.
    /// </remarks>
    private static void AddDeduplicationKey(EntityTypeBuilder<InboxEnvelope> entity) =>
        entity
            .HasIndex(x => new { x.MessageId, x.ConsumerId })
            .IsUnique();

    /// <summary>
    /// Индекс выборки сообщений на обработку.
    /// </summary>
    /// <remarks>
    /// Частичный: у завершённых сообщений ScheduledStartIndexing равен null, поэтому индекс содержит
    /// только необработанные строки и не растёт вместе с историей.
    /// </remarks>
    private static void AddFetchIndex(EntityTypeBuilder<InboxEnvelope> entity) =>
        entity
            .HasIndex(x => new { x.ScheduledStartIndexing, x.Status })
            .HasFilter($"\"{nameof(InboxEnvelope.ScheduledStartIndexing)}\" IS NOT NULL");

    /// <summary>
    /// Индекс выборки, ведущий по MessageType: под свои дискриминаторы.
    /// </summary>
    /// <remarks>
    /// Захват и подсчёт глубины фильтруют по своим дискриминаторам, а MessageType в основном индексе выборки
    /// стоит после ScheduledStartIndexing, поэтому этот фильтр ложится на строки уже после чтения из heap.
    /// В общей таблице это бьёт по холостому опросу: сервис без своих сообщений всё равно прочёсывает весь
    /// бэклог соседа на каждом тике и на каждой реплике. С MessageType впереди фильтр «мои строки» уходит в
    /// Index Cond: подсчёт глубины считается по индексу вместо Seq Scan, а на PostgreSQL 17+ и захват получает
    /// упорядоченный обход с обрывом по LIMIT прямо по этому индексу.
    /// <para>
    /// Существующий индекс (ScheduledStartIndexing, Status) оставлен: на PostgreSQL до 17 упорядоченный захват
    /// с несколькими дискриминаторами идёт по нему, потому что ScalarArrayOp по ведущей колонке там ещё не
    /// отдаёт вывод, упорядоченный по следующей.
    /// </para>
    /// </remarks>
    private static void AddOwnFetchIndex(EntityTypeBuilder<InboxEnvelope> entity) =>
        entity
            .HasIndex(x => new { x.MessageType, x.ScheduledStartIndexing, x.Status })
            .HasFilter($"\"{nameof(InboxEnvelope.ScheduledStartIndexing)}\" IS NOT NULL");

    /// <summary>
    /// Индекс чистки обработанных сообщений.
    /// </summary>
    /// <remarks>
    /// MessageType стоит ДО CreatedUtc, потому что чистка обязана быстро отвечать «своих строк нет»: одну
    /// таблицу могут обслуживать несколько сервисов, и без этого холостой проход (а он случается каждый
    /// час на каждой реплике) вырождается в Seq Scan по всем строкам соседа.
    /// </remarks>
    private static void AddCleanupIndex(EntityTypeBuilder<InboxEnvelope> entity) =>
        entity
            .HasIndex(x => new { x.Status, x.MessageType, x.CreatedUtc });
}
