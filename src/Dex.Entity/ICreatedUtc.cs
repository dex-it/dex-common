using System;

namespace Dex.Entity
{
    public interface ICreatedUtc
    {
#if NET8_0_OR_GREATER
        DateTime CreatedUtc { get; init; }
#else
        DateTime CreatedUtc { get; set; }
#endif
    }
}