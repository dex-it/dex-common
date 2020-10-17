using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "КОНЧАЕТСЯ НА"
    /// </summary>
    [DataContract]
    public class EndsWithOperator : BinaryOperator
    {
        public EndsWithOperator(string typeName, string propertyName, string value)
            : base(typeName, propertyName, value)
        {
        }

        public EndsWithOperator(string typeName, LambdaExpression propertyExpression, string value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            // property.EndsWith(Value)
            return Expression.Call(property, property.Type.GetTypeInfo().GetMethod("EndsWith", new[] { typeof(string) }), Expression.Constant(Value ?? string.Empty));
        }
    }
}