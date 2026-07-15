using System;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Inbox.Ef.Extensions;

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

        // Индекс чистки: покрывает WHERE Status = Succeeded AND CreatedUtc < @stamp ORDER BY CreatedUtc.
        entity
            .HasIndex(x => new { x.Status, x.CreatedUtc });

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
