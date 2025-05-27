using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Dex.Cap.Outbox.Models
{
    [Table(NameConst.TableName, Schema = NameConst.SchemaName)]
    public class OutboxEnvelope
    {
        // ReSharper disable once UnusedMember.Local
        private OutboxEnvelope()
        {
        }

        public OutboxEnvelope(Guid id, Guid correlationId, string messageType, string content, DateTime? startAtUtc = null)
            : this(id, correlationId, messageType, content, startAtUtc, null)
        {
        }

        public OutboxEnvelope(Guid id, Guid correlationId, string messageType, string content, DateTime? startAtUtc, TimeSpan? lockTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(correlationId, Guid.Empty);
            ArgumentException.ThrowIfNullOrEmpty(messageType);
            ArgumentException.ThrowIfNullOrEmpty(content);

            if (lockTimeout.HasValue)
                ArgumentOutOfRangeException.ThrowIfLessThan(lockTimeout.Value, TimeSpan.FromSeconds(10));

            if (startAtUtc.HasValue)
                ArgumentOutOfRangeException.ThrowIfLessThan(startAtUtc.Value, DateTime.UtcNow.AddHours(-1));

            Id = id;
            CorrelationId = correlationId;
            Status = OutboxMessageStatus.New;
            MessageType = messageType;
            Content = content;

            var startDateUtc = startAtUtc ?? DateTime.UtcNow;
            StartAtUtc = startDateUtc;
            ScheduledStartIndexing = startDateUtc;

            LockTimeout = lockTimeout ?? TimeSpan.FromSeconds(30);
            ActivityId = Activity.Current?.Id;
        }

        [Key] public Guid Id { get; set; }

        /// <summary>
        /// Тип сообщения
        /// </summary>
        [Required]
        public string MessageType { get; set; }

        /// <summary>
        /// Сериализованное тело сообщения
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// Идентификатор Activity.Id подсистемы System.Diagnostics.Activity 
        /// </summary>
        public string? ActivityId { get; set; }

        /// <summary>
        /// Кол-во попыток обработать команду
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// Статус сообщения 
        /// </summary>
        [Required]
        public OutboxMessageStatus Status { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Стэк ошибки
        /// </summary>
        public string? Error { get; set; }

        [Required] public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        [Required] public Guid CorrelationId { get; set; }

        public DateTime? StartAtUtc { get; set; }

        public DateTime? ScheduledStartIndexing { get; set; }

        #region Межпроцессная синхронизация

        /// <summary>
        /// Максимально допустимое время удержания блокировки.
        /// </summary>
        /// <remarks>Должен быть больше 10 секунд.</remarks>
        [Required]
        public TimeSpan LockTimeout { get; set; }

        /// <summary>
        /// Уникальный ключ потока который захватил блокировку и только он имеет право освободить блокировку (ключ идемпотентности).
        /// </summary>
        /// <remarks>Допускается игнорирование этого идентификатора если истекло время <see cref="LockExpirationTimeUtc"/>.</remarks>
        public Guid? LockId { get; set; }

        /// <summary>
        /// Максимально допустимый момент времени для удержания блокировки (превентивный таймаут).
        /// </summary>
        /// <remarks>Если превышен этот момент времени то допускается повторный захват блокировки не смотря на значение <see cref="LockId"/>.</remarks>
        public DateTime? LockExpirationTimeUtc { get; set; }

        #endregion
    }
}