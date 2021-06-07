using System;

namespace Dex.DataProvider.Contracts.Entities
{
    public interface IEntity<out TKey>
        where TKey : IComparable
    {
        TKey Id { get; }
    }
}
