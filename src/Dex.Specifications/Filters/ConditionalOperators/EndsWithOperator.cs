using System;
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
            if (property == null) throw new ArgumentNullException(nameof(property));
            
            const string endsWithMethodName = "EndsWith";
            var endsWithMethodInfo = property.Type.GetTypeInfo()?.GetMethod(endsWithMethodName, new[] {typeof(string)});
            if (endsWithMethodInfo == null)
            {
                throw new ArgumentException($"Type \"{property.Type}\" does not contain method \"{endsWithMethodName}\"", nameof(property));
            }
            
            // property.EndsWith(Value)
            return Expression.Call(property, endsWithMethodInfo, Expression.Constant(Value ?? string.Empty));
        }
    }
}