using Dex.DynamicQueryableExtensions.Data;

namespace Dex.DynamicQueryableExtensions.Conditions
{
    /// <summary>
    /// Conditions for filtering, sorting and paging
    /// Apply on IQueryable
    /// </summary>
    public interface IQueryCondition : IPageFilter
    {
        public IFilterCondition[] FilterCondition { get; }
        public IOrderCondition[] SortCondition { get; }
    }
}