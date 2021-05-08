using System;
using System.Collections.Generic;

namespace Dex.Pagination.Conditions
{
    public record QueryCondition : IQueryCondition
    {
        public QueryCondition() : this(null, null)
        {
        }

        public QueryCondition(IPageCondition? pageCondition, params IOrderCondition[]? orderConditions)
        {
            PageCondition = pageCondition ?? new PageCondition(1, 10);
            OrderConditions = orderConditions ?? ArraySegment<IOrderCondition>.Empty;
        }

        public IEnumerable<IOrderCondition> OrderConditions { get; }

        public IPageCondition PageCondition { get; }
    }
}