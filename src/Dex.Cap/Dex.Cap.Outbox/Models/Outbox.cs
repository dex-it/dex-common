using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.Outbox.Models
{
    [Table("_outbox")]
    public class Outbox
    {
        public Outbox()
        {
            Id = Guid.NewGuid();
            CorrelationId = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }

        public Guid CorrelationId { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public int Retries { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public OutboxMessageStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        public string Error { get; set; }

        public DateTime? Updated { get; set; }

        public OutboxMessageType OutboxMessageType { get; set; }
    }
}