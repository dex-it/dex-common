using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "БОЛЬШЕ ИЛИ РАВНО"
    /// </summary>
    [DataContract]
    public class GreaterOrEqualOperator : BinaryOperator
    {
        public GreaterOrEqualOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName, value)
        {
        }

        public GreaterOrEqualOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property >= Value
            return Expression.GreaterThanOrEqual(property, CreateConstant(property.Type, Value));
        }
    }
}