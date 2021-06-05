using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.Outbox.Models
{
    [Table("outbox", Schema = "cap")]
    public class Outbox
    {
        public Outbox(Guid correlationId, string messageType, OutboxMessageStatus status, string content)
        {
            Id = Guid.NewGuid();
            CorrelationId = correlationId;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Status = status;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        [Key] public Guid Id { get; set; }

        public Guid CorrelationId { get; set; }

        [Required] public string MessageType { get; set; }

        [Required] public string Content { get; set; }

        public int Retries { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        [Required] public OutboxMessageStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string? Error { get; set; }

        public DateTime? Updated { get; set; }
    }
}