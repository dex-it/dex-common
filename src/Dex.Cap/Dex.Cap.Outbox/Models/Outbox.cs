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

        [Key] public Guid Id { get; set; }

        public Guid CorrelationId { get; set; }

        [Required] public string MessageType { get; set; }

        [Required]public string Content { get; set; }

        public int Retries { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        [Required]public OutboxMessageStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        public string Error { get; set; }

        public DateTime? Updated { get; set; }
    }
}