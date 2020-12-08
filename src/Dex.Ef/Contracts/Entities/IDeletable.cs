using System;
using Dex.Ef.Attributes;

namespace Dex.Ef.Contracts.Entities
{
    public interface IDeletable
    {
        [Index]
        DateTime? DeletedUtc { get; set; }
    }
}