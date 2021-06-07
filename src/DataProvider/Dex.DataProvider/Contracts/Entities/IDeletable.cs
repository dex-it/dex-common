using System;

namespace Dex.DataProvider.Contracts.Entities
{
    public interface IDeletable
    {
        DateTime? DeletedUtc { get; set; }
    }
}