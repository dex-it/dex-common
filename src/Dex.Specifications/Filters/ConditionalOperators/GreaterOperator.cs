using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "БОЛЬШЕ"
    /// </summary>
    [DataContract]
    public class GreaterOperator : BinaryOperator
    {
        public GreaterOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName, value)
        {
        }

        public GreaterOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property > Value
            return Expression.GreaterThan(property, CreateConstant(property.Type, Value));
        }
    }
}