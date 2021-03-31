namespace Dex.Pagination
{
    public interface IPageFilter
    {
        int Page { get; set; }
        int PageSize { get; set; }
    }
}