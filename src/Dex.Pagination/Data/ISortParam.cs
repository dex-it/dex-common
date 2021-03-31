namespace Dex.Pagination.Data
{
    public interface ISortParam
    {
        string FieldName { get; set; }
        bool IsDesc { get; set; }
    }
}