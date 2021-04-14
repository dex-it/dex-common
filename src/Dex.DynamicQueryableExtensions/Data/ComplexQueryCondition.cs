namespace Dex.DynamicQueryableExtensions.Data
{
    public record ComplexQueryCondition : IComplexQueryCondition
    {
        public IFilterCondition[] FilterCondition { get; set; }
        public ISortCondition[] SortCondition { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}