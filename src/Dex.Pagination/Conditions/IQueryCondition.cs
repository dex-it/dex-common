namespace Dex.Pagination.Conditions
{
    /// <summary>
    /// Conditions for filtering, sorting and paging
    /// Apply on IQueryable
    /// </summary>
    public interface IQueryCondition
    {
        IOrderCondition[] SortCondition { get; }
        IPageCondition PageCondition { get; }
    }
}