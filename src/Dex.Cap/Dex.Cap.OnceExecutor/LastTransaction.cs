using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.OnceExecutor
{
    [Table("_last_operation")]
    public class LastTransaction
    {
        [Key] public Guid IdempotentKey { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}