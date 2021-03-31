namespace Dex.Pagination
{
    public class PageFilter: IPageFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}