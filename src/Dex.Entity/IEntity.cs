#pragma warning disable CA1040
using System;
using System.ComponentModel.DataAnnotations;

namespace Dex.Entity
{
    public interface IEntity
    {
        
    }

    public interface IEntity<out TKey> : IEntity
        where TKey : IComparable
    {
        [Key]
        TKey Id { get; }
    }
}
