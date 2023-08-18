using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Dex.Cap.Outbox.Models
{
    [Table("outbox", Schema = "cap")]
    public class OutboxEnvelope
    {
        public OutboxEnvelope(Guid id, Guid correlationId, string messageType, OutboxMessageStatus status, string content, DateTime? startAtUtc = null)
        {
            var startDateUtc = startAtUtc ?? DateTime.UtcNow;

            Id = id;
            CorrelationId = correlationId;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Status = status;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            StartAtUtc = startDateUtc;
            ScheduledStartIndexing = startDateUtc;
            ActivityId = Activity.Current?.Id;
        }

        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Полное имя типа сообщения, AssemblyQualifiedName.
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

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        [Required]
        public Guid CorrelationId { get; set; }

        public DateTime? StartAtUtc { get; set; }

        public DateTime? ScheduledStartIndexing { get; set; }

        #region Межпроцессная синхронизация

        /// <summary>
        /// Максимально допустимое время удержания блокировки.
        /// </summary>
        /// <remarks>Должен быть больше 10 секунд.</remarks>
        [Required]
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

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