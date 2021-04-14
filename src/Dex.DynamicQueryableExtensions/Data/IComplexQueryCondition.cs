namespace Dex.DynamicQueryableExtensions.Data
{
    /// <summary>
    /// Conditions for filtering, sorting and paging
    /// Apply on IQueryable
    /// </summary>
    public interface IComplexQueryCondition : IPageFilter
    {
        public IFilterCondition[] FilterCondition { get; set; }
        public ISortCondition[] SortCondition { get; set; }
    }
}
