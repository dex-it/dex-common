using System;

namespace Dex.Entity
{
    public interface IDeletable
    {
        DateTime? DeletedUtc { get; set; }
    }
}