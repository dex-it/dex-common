using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dex.Specifications.Filters.BooleanOperators;
using Dex.Specifications.Filters.ConditionalOperators;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Класс для построения дерева условного выражения
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого строится выражение</typeparam>
    class ConditionBuilder<T> : IConditionBuilder<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="booleanOperator">Логический оператор</param>
        /// <exception cref="ArgumentNullException">Логический оператор не задан</exception>
        public ConditionBuilder(BooleanOperator booleanOperator)
        {
            if (booleanOperator == null)
            {
                throw new ArgumentNullException();
            }

            _typeName = typeof(T).AssemblyQualifiedName;
            _operator = booleanOperator;
        }


        private readonly string _typeName;
        private readonly BooleanOperator _operator;


        /// <summary>
        /// Создать условный оператор
        /// </summary>
        public IFilterOperator BuildOperator()
        {
            return _operator;
        }


        public IConditionBuilder<T> Or(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new OrOperator(_typeName)));
            var buildOperator = builder.BuildOperator();

            _operator.Add(buildOperator);

            return this;
        }

        public IConditionBuilder<T> And(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new AndOperator(_typeName)));
            var buildOperator = builder.BuildOperator();

            _operator.Add(buildOperator);

            return this;
        }

        public IConditionBuilder<T> NotOr(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new OrOperator(_typeName)));
            var buildOperator = builder.BuildOperator();

            _operator.Add(new NotOperator(_typeName, buildOperator));

            return this;
        }

        public IConditionBuilder<T> NotAnd(Func<IConditionBuilder<T>, IConditionBuilder<T>> buildExpressionFunc)
        {
            var builder = buildExpressionFunc(new ConditionBuilder<T>(new AndOperator(_typeName)));
            var buildOperator = builder.BuildOperator();

            _operator.Add(new NotOperator(_typeName, buildOperator));

            return this;
        }


        public IConditionBuilder<T> Equal<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(new EqualOperator(_typeName, propertyExpression, value));
            return this;
        }

        public IConditionBuilder<T> NotEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(!new EqualOperator(_typeName, propertyExpression, value));
            return this;
        }


        public IConditionBuilder<T> Null<TProperty>(Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
        {
            _operator.Add(new EqualOperator(_typeName, propertyExpression, null));
            return this;
        }

        public IConditionBuilder<T> NotNull<TProperty>(Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
        {
            _operator.Add(!new EqualOperator(_typeName, propertyExpression, null));
            return this;
        }


        public IConditionBuilder<T> Greater<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(new GreaterOperator(_typeName, propertyExpression, value));
            return this;
        }

        public IConditionBuilder<T> GreaterOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(new GreaterOrEqualOperator(_typeName, propertyExpression, value));
            return this;
        }


        public IConditionBuilder<T> Less<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(new LessOperator(_typeName, propertyExpression, value));
            return this;
        }

        public IConditionBuilder<T> LessOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            _operator.Add(new LessOrEqualOperator(_typeName, propertyExpression, value));
            return this;
        }


        public IConditionBuilder<T> Contains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value)
        {
            _operator.Add(new ContainsOperator(_typeName, propertyExpression, value));
            return this;
        }

        public IConditionBuilder<T> NotContains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value)
        {
            _operator.Add(!new ContainsOperator(_typeName, propertyExpression, value));
            return this;
        }


        public IConditionBuilder<T> StartsWith(Expression<Func<T, string>> propertyExpression, string value)
        {
            _operator.Add(new StartsWithOperator(_typeName, propertyExpression, value));
            return this;
        }

        public IConditionBuilder<T> EndsWith(Expression<Func<T, string>> propertyExpression, string value)
        {
            _operator.Add(new EndsWithOperator(_typeName, propertyExpression, value));
            return this;
        }


        public IConditionBuilder<T> In<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<TProperty> values)
        {
            _operator.Add(new InOperator(_typeName, propertyExpression, values));
            return this;
        }

        public IConditionBuilder<T> NotIn<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<TProperty> values)
        {
            _operator.Add(!new InOperator(_typeName, propertyExpression, values));
            return this;
        }


        public IConditionBuilder<T> Between<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty beginValue, TProperty endValue)
        {
            _operator.Add(new BetweenOperator(_typeName, propertyExpression, beginValue, endValue));
            return this;
        }

        public IConditionBuilder<T> NotBetween<TProperty>(Expression<Func<T, TProperty>> propertyExpression, TProperty beginValue, TProperty endValue)
        {
            _operator.Add(!new BetweenOperator(_typeName, propertyExpression, beginValue, endValue));
            return this;
        }
    }
}