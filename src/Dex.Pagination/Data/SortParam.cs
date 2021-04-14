using System;

namespace Dex.Pagination.Data
{
    public sealed record SortParam : ISortParam
    {
        private readonly string _fieldName = string.Empty;

        public string FieldName
        {
            get => _fieldName;
            init => _fieldName = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool IsDesc { get; init; }

        public bool Equals(SortParam? other)
        {
            if (other == null) return false;
            return other.FieldName == FieldName && other.IsDesc == IsDesc;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FieldName, IsDesc);
        }
    }
}