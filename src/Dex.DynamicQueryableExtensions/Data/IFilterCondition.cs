namespace Dex.DynamicQueryableExtensions.Data
{
    /// <summary>
    /// Условие фильтрации по одному полю
    /// </summary>
    public interface IFilterCondition
    {
        public string FieldName { get; }

        public FilterOperation Operation { get; }

        public string[] Value { get; }
    }
}
