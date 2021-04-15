namespace Dex.DynamicQueryableExtensions.Data
{
    public interface IOrderCondition
    {
        string FieldName { get; }
        bool IsDesc { get; }
    }
}