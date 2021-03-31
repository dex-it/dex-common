namespace Dex.Pagination
{
    public class SortParam : ISortParam
    {
        public string FieldName { get; set; }
        public bool IsDesc { get; set; }
    }
}