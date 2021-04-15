namespace Dex.DynamicQueryableExtensions.Data
{
    /// <summary>
    /// Операция в фильтре
    /// </summary>
    public enum FilterOperation
    {
        /// <summary>
        /// Меньше
        /// </summary>
        LT,

        /// <summary>
        /// Меньше или равно
        /// </summary>
        LE,

        /// <summary>
        /// Равно 
        /// </summary>
        EQ,

        /// <summary>
        /// Больше
        /// </summary>
        GE,

        /// <summary>
        /// Больше или равно
        /// </summary>
        GT,

        /// <summary>
        /// Неравно
        /// </summary>
        NE,

        /// <summary>
        /// Содержит
        /// </summary>
        LK,

        /// <summary>
        /// Содержит регистро-независимый
        /// </summary>
        ILK,

        /// <summary>
        /// Входит
        /// </summary>
        IN,

        /// <summary>
        /// Не входит
        /// </summary>
        NI
    }
}
