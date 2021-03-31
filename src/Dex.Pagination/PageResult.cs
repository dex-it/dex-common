using System.Collections.Generic;

namespace Dex.Pagination
{
    public class PageResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int Count { get; set; }
    }
}