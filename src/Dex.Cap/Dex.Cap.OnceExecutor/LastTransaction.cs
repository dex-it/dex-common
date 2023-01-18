using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dex.Cap.OnceExecutor
{
    [Table(TableName, Schema = "cap")]
    public class LastTransaction
    {
        public const string TableName = "last_transaction";

        public string IdempotentKey { get; set; } = null!;
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}