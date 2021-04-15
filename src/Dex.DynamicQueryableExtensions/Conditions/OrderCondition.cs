

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.DynamicQueryableExtensions.Data
{
    public sealed record OrderCondition : IOrderCondition
    {
        public string FieldName { get; init; } = string.Empty;

        public bool IsDesc { get; init; }
    }
}