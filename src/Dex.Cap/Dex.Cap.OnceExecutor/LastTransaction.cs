using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.OnceExecutor
{
    [Table(TableName, Schema = "cap")]
    public class LastTransaction
    {
        public const string TableName = "last_transaction";
        
        [Key] public Guid IdempotentKey { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}