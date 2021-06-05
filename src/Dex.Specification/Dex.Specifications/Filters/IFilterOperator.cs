using System.Linq.Expressions;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Оператор фильтра
    /// </summary>
    public interface IFilterOperator
    {
        /// <summary>
        /// Тип для фильтрации
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Создать выражение фильтрации
        /// </summary>
        LambdaExpression CreateFilter();

        /// <summary>
        /// Удовлетворяет ли объект условию фильтра
        /// </summary>
        /// <param name="item">Проверяемый объект</param>
        bool IsSatisfiedBy(object item);
    }
}