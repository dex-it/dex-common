using System;
using Dex.Ef.Attributes;

namespace Dex.Ef.Contracts.Entities
{
    public interface IUpdatedUtc
    {
        [Index]
        DateTime UpdatedUtc { get; set; }
    }
}