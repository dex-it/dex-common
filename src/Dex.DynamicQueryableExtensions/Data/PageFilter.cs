namespace Dex.DynamicQueryableExtensions.Data
{
    public record PageFilter : IPageFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}