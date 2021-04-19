

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.Pagination.Conditions
{
    public sealed record OrderCondition : IOrderCondition
    {
        public string FieldName { get; init; } = string.Empty;

        public bool IsDesc { get; init; }
    }
}