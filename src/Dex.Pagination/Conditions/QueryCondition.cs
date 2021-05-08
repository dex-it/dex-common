using System;
using System.Collections.Generic;

namespace Dex.Pagination.Conditions
{
    public record QueryCondition : IQueryCondition
    {
        public IEnumerable<IOrderCondition> SortCondition { get; init; } = Array.Empty<IOrderCondition>();

        public IPageCondition PageCondition { get; init; } = new PageCondition {Page = 1, PageSize = 10};
    }
}