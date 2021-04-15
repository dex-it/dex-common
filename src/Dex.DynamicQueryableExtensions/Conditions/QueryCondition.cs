using Dex.DynamicQueryableExtensions.Data;

namespace Dex.DynamicQueryableExtensions.Conditions
{
    public record QueryCondition : IQueryCondition
    {
        public IFilterCondition[] FilterCondition { get; init; }
        public IOrderCondition[] SortCondition { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}