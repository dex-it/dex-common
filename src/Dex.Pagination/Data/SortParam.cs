using System;

namespace Dex.Pagination.Data
{
    public class SortParam : ISortParam
    {
        public string FieldName { get; set; } = String.Empty;
        public bool IsDesc { get; set; }
    }
}