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
