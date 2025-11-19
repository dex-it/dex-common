using System.Collections.Generic;

namespace Dex.Pagination.Conditions
{
    /// <summary>
    /// Conditions for filtering, sorting and paging
    /// Apply on IQueryable
    /// </summary>
    public interface IQueryCondition
    {
        IEnumerable<IOrderCondition>? OrderConditions { get; }
        IPageCondition? PageCondition { get; }
    }
}