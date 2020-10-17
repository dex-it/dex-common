using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "НАЧИНАЕТСЯ С"
    /// </summary>
    [DataContract]
    public class StartsWithOperator : BinaryOperator
    {
        public StartsWithOperator(string typeName, string propertyName, string value)
            : base(typeName, propertyName, value)
        {
        }

        public StartsWithOperator(string typeName, LambdaExpression propertyExpression, string value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            // property.StartsWith(Value)
            return Expression.Call(property, property.Type.GetTypeInfo().GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant(Value ?? string.Empty));
        }
    }
}