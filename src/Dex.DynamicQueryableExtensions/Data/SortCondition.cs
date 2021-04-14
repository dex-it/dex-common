using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.DynamicQueryableExtensions.Data
{
    public sealed record SortCondition : ISortCondition
    {
        private readonly string _fieldName = string.Empty;

        public string FieldName
        {
            get => _fieldName;
            init => _fieldName = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool IsDesc { get; init; }

        public bool Equals(SortCondition other)
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