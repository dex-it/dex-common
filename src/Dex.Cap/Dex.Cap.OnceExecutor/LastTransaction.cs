using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.OnceExecutor
{
    [Table("last_transaction", Schema = "cap")]
    public class LastTransaction
    {
        [Key] public Guid IdempotentKey { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}