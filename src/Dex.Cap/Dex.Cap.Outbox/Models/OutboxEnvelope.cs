using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.Outbox.Models
{
    [Table("outbox", Schema = "cap")]
    public class OutboxEnvelope
    {
        public OutboxEnvelope(Guid id, string messageType, OutboxMessageStatus status, string content)
        {
            Id = id;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Status = status;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        [Key] 
        public Guid Id { get; set; }

        [Required] 
        public string MessageType { get; set; }

        [Required] 
        public string Content { get; set; }

        public int Retries { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Required] 
        public OutboxMessageStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string? Error { get; set; }

        public DateTime? Updated { get; set; }

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