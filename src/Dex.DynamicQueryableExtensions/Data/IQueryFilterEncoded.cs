namespace Dex.DynamicQueryableExtensions.Data
{
    /// <summary>
    /// IQueryFilter encoded Base64 JSON
    /// </summary>
    public interface IQueryFilterEncoded
    {
        /// <summary>
        /// encoded filter JSON string 
        /// </summary>
        public string EncodedFilter { get; set; }

        /// <summary>
        /// Decode filter object from string
        /// </summary>
        /// <returns></returns>
        IComplexQueryCondition DecodeFilter();
    }
}