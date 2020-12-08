using System;
using Dex.Ef.Attributes;

namespace Dex.Ef.Contracts.Entities
{
    public interface ICreatedUtc
    {
        [Index]
        DateTime CreatedUtc { get; set; }
    }
}