using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Интерфейс для построения дерева условного выражения
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого строится выражение</typeparam>
    public interface IConditionBuilder<T>
    {
        IFilterOperator BuildOperator();

        IConditionBuilder<T> Or(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IConditionBuilder<T> And(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IConditionBuilder<T> NotOr(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);
        IConditionBuilder<T> NotAnd(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc);

        IConditionBuilder<T> Equal<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);
        IConditionBuilder<T> NotEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);

        IConditionBuilder<T> Null<TProperty>(Expression<Func<T, TProperty>> propertyExpression) where TProperty : class;
        IConditionBuilder<T> NotNull<TProperty>(Expression<Func<T, TProperty>> propertyExpression) where TProperty : class;

        IConditionBuilder<T> Greater<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);
        IConditionBuilder<T> GreaterOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);

        IConditionBuilder<T> Less<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);
        IConditionBuilder<T> LessOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value);

        IConditionBuilder<T> Contains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value);
        IConditionBuilder<T> NotContains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value);

        IConditionBuilder<T> StartsWith(Expression<Func<T, string>> propertyExpression, string value);
        IConditionBuilder<T> EndsWith(Expression<Func<T, string>> propertyExpression, string value);

        IConditionBuilder<T> In<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<TProperty> values);
        IConditionBuilder<T> NotIn<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<TProperty> values);

        IConditionBuilder<T> Between<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty beginValue, TProperty endValue);
        IConditionBuilder<T> NotBetween<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty beginValue, TProperty endValue);
    }
}