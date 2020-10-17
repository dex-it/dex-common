using System;
using Dex.Specifications.Filters.BooleanOperators;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Класс для создания условного оператора
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого создается условный оператор</typeparam>
    class FilterBuilder<T> : IFilterBuilder<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public FilterBuilder()
        {
            _typeName = typeof(T).AssemblyQualifiedName;
        }


        private readonly string _typeName;


        public IFilterOperator Or(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new OrOperator(_typeName)));
            return builder.BuildOperator();
        }

        public IFilterOperator And(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new AndOperator(_typeName)));
            return builder.BuildOperator();
        }

        public IFilterOperator NotOr(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            return new NotOperator(_typeName, Or(buildExpressionFunc));
        }

        public IFilterOperator NotAnd(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            return new NotOperator(_typeName, And(buildExpressionFunc));
        }
    }
}