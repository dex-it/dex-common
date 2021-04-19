namespace Dex.Pagination.Conditions
{
    /// <summary>
    /// Conditions for filtering, sorting and paging
    /// Apply on IQueryable
    /// </summary>
    public interface IQueryCondition
    {
        public IOrderCondition[] SortCondition { get; }
        public IPageCondition PageCondition { get; }
    }
}