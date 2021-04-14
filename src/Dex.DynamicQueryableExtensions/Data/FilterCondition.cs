using System;
using System.Linq;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.DynamicQueryableExtensions.Data
{
    public sealed record FilterCondition: IFilterCondition
    {
        public string FieldName { get; init; }
        public FilterOperation Operation { get; init; }
        public string[] Value { get; init; }

        public bool Equals(FilterCondition other)
        {
            if (other == null) return false;
            return other.Operation == Operation && other.FieldName == FieldName && other.Value.SequenceEqual(Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FieldName, (int) Operation, Value);
        }
    }
}