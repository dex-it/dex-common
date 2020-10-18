using System;
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
            if (property == null) throw new ArgumentNullException(nameof(property));
            
            const string startsWithMethodName = "StartsWith";
            var startsWithMethodInfo = property.Type.GetTypeInfo()?.GetMethod(startsWithMethodName, new[] {typeof(string)});
            if (startsWithMethodInfo == null)
            {
                throw new ArgumentException($"Type \"{property.Type}\" does not contain method \"{startsWithMethodName}\"", nameof(property));
            }

            
            // property.StartsWith(Value)
            return Expression.Call(property, startsWithMethodInfo, Expression.Constant(Value ?? string.Empty));
        }
    }
}