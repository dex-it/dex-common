namespace Dex.Pagination.Conditions
{
    public record PageCondition : IPageCondition
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}