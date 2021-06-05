using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "РАВНО"
    /// </summary>
    [DataContract]
    public class EqualOperator : BinaryOperator
    {
        public EqualOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName, value)
        {
        }

        public EqualOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property == Value
            return Expression.Equal(property, CreateConstant(property.Type, Value));
        }
    }
}