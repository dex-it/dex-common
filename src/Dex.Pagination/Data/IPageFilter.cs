namespace Dex.Pagination.Data
{
    public interface IPageFilter
    {
        int Page { get; set; }
        int PageSize { get; set; }
    }
}