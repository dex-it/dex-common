using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "СОДЕРЖИТ"
    /// </summary>
    [DataContract]
    public class ContainsOperator : BinaryOperator
    {
        public ContainsOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName, value)
        {
        }

        public ContainsOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property.Contains(Value)

            Expression result = null;

            var propertyType = property.Type;
            var containsMethod = propertyType.GetTypeInfo().GetMethod("Contains");

            if (containsMethod != null)
            {
                // Если тип свойства имеет метод Contains()
                result = Expression.Call(property, containsMethod, Expression.Constant(Value));
            }
            else if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(propertyType) && propertyType.GetTypeInfo().IsGenericType)
            {
                // Поиск метода расширения Contains() для типа свойства
                containsMethod = typeof(Enumerable).GetTypeInfo().GetMethods().Where(method => method.Name == "Contains")
                    .Select(method => new { method, parameters = method.GetParameters() })
                    .Where(m => m.parameters.Length == 2)
                    .Select(m => m.method)
                    .FirstOrDefault();

                if (containsMethod != null)
                {
                    containsMethod = containsMethod.MakeGenericMethod(propertyType.GetTypeInfo().GetGenericArguments());
                    result = Expression.Call(containsMethod, property, Expression.Constant(Value));
                }
            }

            if (result == null)
            {
                throw new NotSupportedException();
            }

            return result;
        }
    }
}