using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using JetBrains.Annotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Dex.Cap.Inbox.Models;

/// <summary>
/// Входящее сообщение, сохранённое до подтверждения источнику.
/// </summary>
/// <remarks>
/// Дедупликация выполняется по паре <see cref="MessageId"/> + <see cref="ConsumerId"/>:
/// одно и то же сообщение может быть легитимно обработано разными потребителями,
/// поэтому идентификатора сообщения самого по себе недостаточно.
/// </remarks>
[Table(NameConst.TableName, Schema = NameConst.SchemaName)]
public class InboxEnvelope
{
    /// <summary>
    /// Максимальная длина <see cref="MessageId"/> и <see cref="ConsumerId"/>.
    /// </summary>
    /// <remarks>Ограничение нужно, потому что обе колонки входят в уникальный индекс.</remarks>
    public const int MaxIdentityLength = 256;

    [UsedImplicitly]
    private InboxEnvelope()
    {
    }

    public InboxEnvelope(Guid id, string messageId, string consumerId, string messageType, string content, TimeSpan? lockTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerId);
        ArgumentException.ThrowIfNullOrEmpty(messageType);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(messageId.Length, MaxIdentityLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(consumerId.Length, MaxIdentityLength);

        if (lockTimeout.HasValue)
            ArgumentOutOfRangeException.ThrowIfLessThan(lockTimeout.Value, TimeSpan.FromSeconds(10));

        Id = id;
        MessageId = messageId;
        ConsumerId = consumerId;
        MessageType = messageType;
        Content = content;
        Status = InboxMessageStatus.New;

        var now = DateTime.UtcNow;
        StartAtUtc = now;
        ScheduledStartIndexing = now;

        LockTimeout = lockTimeout ?? TimeSpan.FromSeconds(30);
        ActivityId = Activity.Current?.Id;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор сообщения в источнике: MessageId шины, Idempotency-Key HTTP-запроса и т.п.
    /// </summary>
    /// <remarks>Задаётся вызывающей стороной, потому что ядро не знает про транспорт.</remarks>
    [Required]
    [MaxLength(MaxIdentityLength)]
    public string MessageId { get; set; }

    /// <summary>
    /// Идентификатор потребителя, для которого принято сообщение.
    /// </summary>
    /// <remarks>Разделяет обработку одного сообщения разными потребителями в пределах одного хранилища.</remarks>
    [Required]
    [MaxLength(MaxIdentityLength)]
    public string ConsumerId { get; set; }

    /// <summary>
    /// Тип сообщения (дискриминатор).
    /// </summary>
    [Required]
    public string MessageType { get; set; }

    /// <summary>
    /// Сериализованное тело сообщения.
    /// </summary>
    [Required]
    public string Content { get; set; }

    /// <summary>
    /// Идентификатор Activity.Id подсистемы System.Diagnostics.Activity.
    /// </summary>
    /// <remarks>Сохраняется при приёме, чтобы фоновая обработка продолжила трассу источника, а не начала свою.</remarks>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Кол-во выполненных попыток обработки.
    /// </summary>
    public int Retries { get; set; }

    /// <summary>
    /// Статус сообщения.
    /// </summary>
    [Required]
    public InboxMessageStatus Status { get; set; }

    /// <summary>
    /// Сообщение об ошибке последней неудачной попытки.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Стэк ошибки последней неудачной попытки.
    /// </summary>
    public string? Error { get; set; }

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? Updated { get; set; }

    /// <summary>
    /// Момент, начиная с которого сообщение можно обрабатывать (сдвигается ретрай-стратегией).
    /// </summary>
    public DateTime? StartAtUtc { get; set; }

    /// <summary>
    /// Копия <see cref="StartAtUtc"/> для покрывающего индекса выборки.
    /// </summary>
    /// <remarks>Сбрасывается в null у завершённых сообщений, чтобы вывести их из выборки и из индекса.</remarks>
    public DateTime? ScheduledStartIndexing { get; set; }

    #region Межпроцессная синхронизация

    /// <summary>
    /// Максимально допустимое время удержания блокировки.
    /// </summary>
    /// <remarks>Должен быть больше 10 секунд и превышать время обработки сообщения, иначе возможна повторная обработка.</remarks>
    [Required]
    public TimeSpan LockTimeout { get; set; }

    /// <summary>
    /// Уникальный ключ потока который захватил блокировку и только он имеет право её освободить (ключ идемпотентности).
    /// </summary>
    /// <remarks>Допускается игнорирование этого идентификатора если истекло время <see cref="LockExpirationTimeUtc"/>.</remarks>
    public Guid? LockId { get; set; }

    /// <summary>
    /// Максимально допустимый момент времени для удержания блокировки (превентивный тайм-аут).
    /// </summary>
    /// <remarks>
    /// Заменяет отдельный статус "в обработке": если процесс умер, аренда истечёт и сообщение вернётся в выборку,
    /// тогда как статус "в обработке" остался бы висеть навсегда.
    /// </remarks>
    public DateTime? LockExpirationTimeUtc { get; set; }

    #endregion
}
