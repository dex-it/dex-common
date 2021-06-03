using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC.Core.Consistent.OnceExecutor
{
    [Table("_Last")]
    public class LastTransaction
    {
        [Key] public Guid Last { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}