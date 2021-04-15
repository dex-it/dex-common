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
    }
}