using System;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Интерфейс для создания условного оператора
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого создается условный оператор</typeparam>
    public interface IFilterBuilder<T>
    {
        IFilterOperator Or(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IFilterOperator And(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IFilterOperator NotOr(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IFilterOperator NotAnd(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
    }
}