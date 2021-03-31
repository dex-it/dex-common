namespace Dex.Pagination
{
    public interface ISortParam
    {
        string FieldName { get; set; }
        bool IsDesc { get; set; }
    }
}