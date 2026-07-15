using System;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;

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

        // Ключ дедупликации. Уникальность обязана быть на уровне БД: она же обслуживает
        // ON CONFLICT DO NOTHING при приёме сообщения и разруливает гонку конкурентных доставок.
        entity
            .HasIndex(x => new { x.MessageId, x.ConsumerId })
            .IsUnique();

        // Индекс выборки. Частичный: у завершённых сообщений ScheduledStartIndexing = NULL, поэтому
        // индекс содержит только необработанные строки и не растёт вместе с историей.
        entity
            .HasIndex(x => new { x.ScheduledStartIndexing, x.Status })
            .HasFilter($"\"{nameof(InboxEnvelope.ScheduledStartIndexing)}\" IS NOT NULL");

        // Индекс чистки: WHERE Status = Succeeded AND MessageType = ANY(@own) AND CreatedUtc < @stamp.
        // MessageType стоит ДО CreatedUtc, потому что чистка обязана быстро отвечать «своих строк нет»:
        // одну таблицу могут обслуживать несколько сервисов, и без него холостой проход (а он случается
        // каждый час на каждой реплике) вырождается в Seq Scan по всем строкам соседа.
        entity
            .HasIndex(x => new { x.Status, x.MessageType, x.CreatedUtc });

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
}
