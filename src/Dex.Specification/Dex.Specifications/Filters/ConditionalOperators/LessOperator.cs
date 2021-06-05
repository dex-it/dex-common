using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "МЕНЬШЕ"
    /// </summary>
    [DataContract]
    public class LessOperator : BinaryOperator
    {
        public LessOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName, value)
        {
        }

        public LessOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property < Value
            return Expression.LessThan(property, CreateConstant(property.Type, Value));
        }
    }
}