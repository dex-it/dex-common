using System;
using System.ComponentModel.DataAnnotations;

namespace Dex.Ef.Contracts.Entities
{
    public interface IEntity
    {
        
    }

    public interface IDbEntity
    {
    }

    public interface IEntity<out TKey> : IEntity
        where TKey : IComparable
    {
        [Key]
        TKey Id { get; }
    }
}
