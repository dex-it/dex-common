namespace Dex.Pagination.Data
{
    public interface ISortParam
    {
        string FieldName { get; }
        bool IsDesc { get; }
    }
}