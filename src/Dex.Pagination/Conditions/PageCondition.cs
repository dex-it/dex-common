using System;

namespace Dex.Pagination.Conditions
{
    public record PageCondition : IPageCondition
    {
        public PageCondition(int page, int pageSize)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "must be greater 0");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "must be greater 0");

            Page = page;
            PageSize = pageSize;
        }

        public int Page { get; }
        public int PageSize { get; }
    }
}