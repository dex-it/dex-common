namespace Dex.DynamicQueryableExtensions.Data
{
    public interface ISortCondition
    {
        string FieldName { get; }
        bool IsDesc { get; }
    }
}