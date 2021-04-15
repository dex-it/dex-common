namespace Dex.DynamicQueryableExtensions.Data
{
    public interface IPageFilter
    {
        int Page { get; }
        int PageSize { get; }
    }
}