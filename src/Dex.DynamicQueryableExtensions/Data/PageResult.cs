using System;
using System.Collections.Generic;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.DynamicQueryableExtensions.Data
{
    public record PageResult<T>
    {
        public IEnumerable<T> Items { get; set; } = ArraySegment<T>.Empty;

        public int? Count { get; set; }
    }
}